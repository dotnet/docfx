// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, List<(InternalXrefSpec specs, Document file)>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;
        private readonly Context _context;

        private static ThreadLocal<Stack<(string uid, string propertyName, Document parent)>> t_recursionDetector = new ThreadLocal<Stack<(string, string, Document)>>(() => new Stack<(string, string, Document)>());

        public (Error error, string href, string display, Document referencedFile) Resolve(string uid, SourceInfo<string> href, string displayPropertyName, Document relativeTo, Document rootFile, string moniker = null)
        {
            if (t_recursionDetector.Value.Contains((uid, displayPropertyName, relativeTo)))
            {
                var referenceMap = t_recursionDetector.Value.Select(x => x.parent).ToList();
                referenceMap.Reverse();
                referenceMap.Add(relativeTo);
                throw Errors.CircularReference(referenceMap).ToException();
            }

            try
            {
                t_recursionDetector.Value.Push((uid, displayPropertyName, relativeTo));
                return ResolveCore(uid, href, displayPropertyName, rootFile, moniker);
            }
            finally
            {
                Debug.Assert(t_recursionDetector.Value.Count > 0);
                t_recursionDetector.Value.Pop();
            }
        }

        private (Error error, string href, string display, Document referencedFile) ResolveCore(string uid, SourceInfo<string> href, string displayPropertyName, Document rootFile, string moniker = null)
        {
            string name = null;
            string displayPropertyValue = null;
            string resolvedHref = null;

            if (TryResolveFromInternal(uid, href, rootFile.FilePath, moniker, out var internalXrefSpec, out var referencedFile))
            {
                var (_, query, fragment) = HrefUtility.SplitHref(internalXrefSpec.Href);
                resolvedHref = HrefUtility.MergeHref(RebaseResolvedHref(rootFile, referencedFile), query, fragment.Length == 0 ? "" : fragment.Substring(1));
                name = internalXrefSpec.GetName();
                displayPropertyValue = internalXrefSpec.GetXrefPropertyValue(displayPropertyName);
            }
            else if (TryResolveFromExternal(uid, out var xrefSpec))
            {
                resolvedHref = RemoveHostnameIfSharingTheSameOne(xrefSpec.Href);
                name = xrefSpec.GetName();
                displayPropertyValue = xrefSpec.GetXrefPropertyValue(displayPropertyName);
            }
            else
            {
                return (Errors.UidNotFound(rootFile.FilePath, uid, href), null, null, null);
            }

            // fallback order:
            // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
            return (null, resolvedHref, display, referencedFile);

            string RemoveHostnameIfSharingTheSameOne(string input)
            {
                var hostname = rootFile.Docset.Config.BaseUrl;
                if (input.StartsWith(hostname, StringComparison.OrdinalIgnoreCase))
                {
                    return input.Substring(hostname.Length);
                }
                return input;
            }
        }

        private string RebaseResolvedHref(Document rootFile, Document referencedFile)
            => _context.DependencyResolver.GetRelativeUrl(rootFile, referencedFile);

        private bool TryResolveFromInternal(string uid, SourceInfo<string> href, string file, string moniker, out InternalXrefSpec internalXrefSpec, out Document referencedFile)
        {
            internalXrefSpec = null;
            referencedFile = null;
            if (_internalXrefMap.TryGetValue(uid, out var internalSpecs))
            {
                (internalXrefSpec, referencedFile) = GetInternalSpec(uid, href, file, moniker, internalSpecs);
                if (internalXrefSpec is null)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private bool TryResolveFromExternal(string uid, out XrefSpec spec)
        {
            if (_externalXrefMap.TryGetValue(uid, out spec) && spec != null)
            {
                return true;
            }
            return false;
        }

        private (InternalXrefSpec internalSpec, Document referencedFile) GetInternalSpec(string uid, SourceInfo<string> href, string file, string moniker, List<(InternalXrefSpec, Document)> internalSpecs)
        {
            if (!TryGetValidXrefSpecs(uid, internalSpecs, out var validInternalSpecs))
                return default;

            if (!string.IsNullOrEmpty(moniker))
            {
                foreach (var (internalSpec, doc) in validInternalSpecs)
                {
                    if (internalSpec.Monikers.Contains(moniker))
                    {
                        return (internalSpec, doc);
                    }
                }

                // if the moniker is not defined with the uid
                // log a warning and take the one with latest version
                _context.Report.Write(Errors.InvalidUidMoniker(moniker, uid, file, href));
                return GetLatestInternalXrefMap(validInternalSpecs);
            }

            // For uid with and without moniker range, take the one without moniker range
            var (uidWithoutMoniker, referencedFile) = validInternalSpecs.SingleOrDefault(item => item.spec.Monikers.Count == 0);
            if (uidWithoutMoniker != null)
            {
                return (uidWithoutMoniker, referencedFile);
            }

            // For uid with moniker range, take the latest moniker if no moniker defined while resolving
            if (internalSpecs.Count > 1)
            {
                return GetLatestInternalXrefMap(validInternalSpecs);
            }
            else
            {
                return validInternalSpecs.Single();
            }
        }

        public static XrefMap Create(Context context, Docset docset)
        {
            Dictionary<string, XrefSpec> map = new Dictionary<string, XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var (_, content, _) = RestoreMap.GetRestoredFileContent(docset, url);
                XrefMapModel xrefMap = new XrefMapModel();
                if (url?.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) != false)
                {
                    xrefMap = YamlUtility.Deserialize<XrefMapModel>(content);
                }
                else
                {
                    xrefMap = JsonUtility.Deserialize<XrefMapModel>(content);
                }
                foreach (var spec in xrefMap.References)
                {
                    map[spec.Uid] = spec;
                }
            }
            return new XrefMap(context, map, CreateInternalXrefMap(context, docset.ScanScope));
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(ExpandInternalXrefSpecs());
            context.Output.WriteJson(models, "xrefmap.json");
        }

        private IEnumerable<XrefSpec> ExpandInternalXrefSpecs()
        {
            var loadedInternalSpecs = new List<XrefSpec>();
            foreach (var (uid, specsWithSameUid) in _internalXrefMap)
            {
                if (TryGetValidXrefSpecs(uid, specsWithSameUid, out var validInternalSpecs))
                {
                    var (internalSpec, referencedFile) = GetLatestInternalXrefMap(validInternalSpecs);
                    loadedInternalSpecs.Add(internalSpec.ToExternalXrefSpec(_context, referencedFile));
                }
            }
            return loadedInternalSpecs;
        }

        private (InternalXrefSpec spec, Document referencedFile) GetLatestInternalXrefMap(List<(InternalXrefSpec spec, Document referencedFile)> specs)
            => specs.OrderByDescending(item => item.spec.Monikers.FirstOrDefault(), _context.MonikerProvider.Comparer).FirstOrDefault();

        private bool TryGetValidXrefSpecs(string uid, List<(InternalXrefSpec spec, Document file)> specsWithSameUid, out List<(InternalXrefSpec spec, Document file)> validSpecs)
        {
            validSpecs = new List<(InternalXrefSpec, Document)>();

            // no conflicts
            if (specsWithSameUid.Count() == 1)
            {
                validSpecs.AddRange(specsWithSameUid);
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = specsWithSameUid.Where(item => item.spec.Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.spec.Href);
                _context.Report.Write(Errors.UidConflict(uid, orderedConflict.Select(x => x.file.FilePath)));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single());
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.spec.Monikers.Count > 0);
            if (CheckOverlappingMonikers(specsWithSameUid.Select(x => x.spec), out var overlappingMonikers))
            {
                _context.Report.Write(Errors.MonikerOverlapping(overlappingMonikers));
                return false;
            }

            // define same uid with non-overlapping monikers, add them all
            else
            {
                validSpecs.AddRange(conflictsWithMoniker);
                return true;
            }
        }

        private bool CheckOverlappingMonikers(IEnumerable<InternalXrefSpec> specsWithSameUid, out HashSet<string> overlappingMonikers)
        {
            bool isOverlapping = false;
            overlappingMonikers = new HashSet<string>();
            var monikerHashSet = new HashSet<string>();
            foreach (var spec in specsWithSameUid)
            {
                foreach (var moniker in spec.Monikers)
                {
                    if (!monikerHashSet.Add(moniker))
                    {
                        overlappingMonikers.Add(moniker);
                        isOverlapping = true;
                    }
                }
            }
            return isOverlapping;
        }

        private static IReadOnlyDictionary<string, List<(InternalXrefSpec, Document)>>
            CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<(InternalXrefSpec, Document)>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                return xrefsByUid.ToList().OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value.ToList());
            }
        }

        private XrefMap(Context context, IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, List<(InternalXrefSpec, Document)>> internalXrefMap)
        {
            _context = context;
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
        }

        private static void Load(
            Context context,
            ConcurrentDictionary<string, ConcurrentBag<(InternalXrefSpec, Document)>> xrefsByUid,
            Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                var callStack = new List<Document> { file };
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
                    errors.AddRange(yamlHeaderErrors);

                    var (fileMetaErrors, fileMetadata) = context.MetadataProvider.GetInputMetadata<InputMetadata>(file, yamlHeader);
                    errors.AddRange(fileMetaErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        var (error, spec, _) = LoadMarkdown(context, fileMetadata, file);
                        errors.AddIfNotNull(error);
                        TryAddXref(xrefsByUid, fileMetadata.Uid, file, spec);
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Parse(file, context);
                    errors.AddRange(yamlErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    foreach (var spec in specs)
                    {
                        TryAddXref(xrefsByUid, spec.Uid, file, spec);
                    }
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Parse(file, context);
                    errors.AddRange(jsonErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    foreach (var spec in specs)
                    {
                        TryAddXref(xrefsByUid, spec.Uid, file, spec);
                    }
                }
                context.Report.Write(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(file.ToString(), dex.Error);
            }
            catch
            {
                Console.WriteLine($"Load {file.FilePath} xref failed");
                throw;
            }
        }

        private static (Error error, InternalXrefSpec spec, Document doc) LoadMarkdown(Context context, InputMetadata metadata, Document file)
        {
            var xref = new InternalXrefSpec
            {
                Uid = metadata.Uid,
                Href = file.CanonicalUrlWithoutLocale,
                ReferencedFile = file,
            };
            xref.ExtensionData["name"] = new Lazy<JValue>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title));

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref, file);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(Context context, JObject obj, Document file)
        {
            var uidToJsonPath = new Dictionary<string, string>();
            var jsonPathToUid = new Dictionary<string, string>();
            GetUids(context, file.FilePath, obj, uidToJsonPath, jsonPathToUid);
            if (uidToJsonPath.Count == 0)
            {
                return (new List<Error>(), new List<InternalXrefSpec>());
            }
            var extensionData = new Dictionary<string, Lazy<JValue>>();

            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();
            var (schemaErrors, _) = JsonUtility.ToObject(
                obj,
                schema.Type,
                transform: AttributeTransformer.TransformXref(context, file, null, extensionData));

            errors.AddRange(schemaErrors);

            var extensionDataByUid = new Dictionary<string, (bool isRoot, Dictionary<string, Lazy<JValue>> properties)>();

            foreach (var (uid, value) in uidToJsonPath)
            {
                extensionDataByUid.Add(uid, (string.IsNullOrEmpty(value), new Dictionary<string, Lazy<JValue>>()));
            }

            foreach (var (jsonPath, xrefProperty) in extensionData)
            {
                var (uid, resolvedJsonPath) = MatchExtensionDataToUid(jsonPath);
                if (extensionDataByUid.ContainsKey(uid))
                {
                    var (_, properties) = extensionDataByUid[uid];
                    properties.Add(resolvedJsonPath, xrefProperty);
                }
                else
                {
                    extensionDataByUid.Add(uid, (string.IsNullOrEmpty(uidToJsonPath[uid]), new Dictionary<string, Lazy<JValue>> { { resolvedJsonPath, xrefProperty } }));
                }
            }

            var specs = extensionDataByUid.Select(item =>
            {
                var (isRoot, properties) = item.Value;
                var xref = new InternalXrefSpec
                {
                    Uid = item.Key,
                    Href = isRoot ? file.CanonicalUrlWithoutLocale : $"{file.CanonicalUrlWithoutLocale}#{GetBookmarkFromUid(item.Key)}",
                    ReferencedFile = file,
                };
                xref.ExtensionData.AddRange(properties);
                return xref;
            }).ToList();

            return (errors, specs);

            string GetBookmarkFromUid(string uid)
                => Regex.Replace(uid, @"\W", "_");

            (string uid, string jsonPath) MatchExtensionDataToUid(string jsonPath)
            {
                string subString;
                var index = jsonPath.LastIndexOf('.');
                if (index == -1)
                {
                    subString = string.Empty;
                }
                else
                {
                    subString = jsonPath.Substring(0, index);
                }

                return jsonPathToUid.ContainsKey(subString) ? (jsonPathToUid[subString], jsonPath.Substring(index + 1)) : MatchExtensionDataToUid(subString);
            }
        }

        private static void GetUids(Context context, string filePath, JObject token, Dictionary<string, string> uidToJsonPath, Dictionary<string, string> jsonPathToUid)
        {
            if (token is null)
                return;

            if (token.TryGetValue("uid", out var value) && value is JValue v && v.Value is string str)
            {
                if (!uidToJsonPath.TryAdd(str, token.Path))
                {
                    context.Report.Write(filePath, Errors.UidConflict(str));
                }
                else
                {
                    jsonPathToUid.TryAdd(token.Path, str);
                }
            }

            foreach (var item in token.Children())
            {
                var property = item as JProperty;
                if (property.Value is JObject obj)
                {
                    GetUids(context, filePath, obj, uidToJsonPath, jsonPathToUid);
                }

                if (property.Value is JArray array)
                {
                    foreach (var child in array.Children())
                    {
                        GetUids(context, filePath, child as JObject, uidToJsonPath, jsonPathToUid);
                    }
                }
            }
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<(InternalXrefSpec, Document)>> xrefsByUid, string uid, Document file, InternalXrefSpec spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<(InternalXrefSpec, Document)>()).Add((spec, file));
        }
    }
}
