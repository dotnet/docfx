// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, List<(Lazy<(List<Error>, InternalXrefSpec)> specs, Document file)>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;
        private readonly Context _context;

        private static ThreadLocal<Stack<(string uid, string propertyName, Document parent)>> t_recursionDetector = new ThreadLocal<Stack<(string, string, Document)>>(() => new Stack<(string, string, Document)>());

        public (Error error, string href, string display, Document referencedFile) Resolve(string uid, string href, string displayPropertyName, Document relativeTo, Document rootFile, string moniker = null)
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

        private (Error error, string href, string display, Document referencedFile) ResolveCore(string uid, string href, string displayPropertyName, Document rootFile, string moniker = null)
        {
            string name = null;
            string displayPropertyValue = null;
            string resolvedHref = null;

            if (TryResolveFromInternal(uid, moniker, out var internalXrefSpec, out var referencedFile))
            {
                resolvedHref = RebaseResolvedHref(rootFile, referencedFile);
                name = internalXrefSpec.GetName();
                displayPropertyValue = internalXrefSpec.GetXrefPropertyValue(displayPropertyName);
            }
            else if (TryResolveFromExternal(uid, out var xrefSpec))
            {
                resolvedHref = xrefSpec.Href;
                name = xrefSpec.GetName();
                displayPropertyValue = xrefSpec.GetXrefPropertyValue(displayPropertyName);
            }
            else
            {
                return (Errors.UidNotFound(rootFile, uid, href), null, null, null);
            }

            // fallback order:
            // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
            return (null, resolvedHref, display, referencedFile);
        }

        private string RebaseResolvedHref(Document rootFile, Document referencedFile)
            => _context.DependencyResolver.GetRelativeUrl(rootFile, referencedFile);

        private bool TryResolveFromInternal(string uid, string moniker, out InternalXrefSpec internalXrefSpec, out Document referencedFile)
        {
            internalXrefSpec = null;
            referencedFile = null;
            if (_internalXrefMap.TryGetValue(uid, out var internalSpecs))
            {
                (internalXrefSpec, referencedFile) = GetInternalSpec(uid, moniker, internalSpecs);
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

        private (InternalXrefSpec internalSpec, Document referencedFile) GetInternalSpec(string uid, string moniker, List<(Lazy<(List<Error>, InternalXrefSpec)>, Document)> internalSpecs)
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
                _context.Report.Write(Errors.InvalidUidMoniker(moniker, uid));
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
                var (_, path) = docset.GetFileRestorePath(url);
                var content = File.ReadAllText(path);
                XrefMapModel xrefMap = new XrefMapModel();
                if (url.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
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

        private bool TryGetValidXrefSpecs(string uid, List<(Lazy<(List<Error> errors, InternalXrefSpec spec)> value, Document file)> specsWithSameUid, out List<(InternalXrefSpec spec, Document file)> validSpecs)
        {
            var loadedSpecs = specsWithSameUid.Select(item => LoadXrefSpec(item));
            validSpecs = new List<(InternalXrefSpec, Document)>();

            // no conflicts
            if (loadedSpecs.Count() == 1)
            {
                validSpecs.AddRange(loadedSpecs);
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = loadedSpecs.Where(item => item.Item1.Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.Item1.Href);
                _context.Report.Write(Errors.UidConflict(uid, orderedConflict.Select(x => x.Item2.FilePath)));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single());
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => LoadXrefSpec(x).Item1.Monikers.Count > 0).Select(item => LoadXrefSpec(item));
            if (CheckOverlappingMonikers(loadedSpecs.Select(x => x.Item1), out var overlappingMonikers))
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

            (InternalXrefSpec, Document) LoadXrefSpec((Lazy<(List<Error>, InternalXrefSpec)> value, Document file) input)
            {
                if (input.value is null)
                    return default;

                var isValueCreated = input.value.IsValueCreated;
                var (errors, spec) = input.value.Value;
                if (!isValueCreated)
                {
                    foreach (var error in errors)
                    {
                        _context.Report.Write(error);
                    }

                    // Sort monikers descending by moniker definition order
                    if (spec.Monikers.Count > 1)
                    {
                        var orderedMonikers = spec.Monikers.OrderBy(item => item, _context.MonikerProvider.Comparer).ToHashSet();
                        spec.Monikers = orderedMonikers;
                    }
                }
                return (spec, input.file);
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

        private static IReadOnlyDictionary<string, List<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>>
            CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                return xrefsByUid.ToList().OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value.ToList());
            }
        }

        private XrefMap(Context context, IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, List<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>> internalXrefMap)
        {
            _context = context;
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
        }

        private static void Load(
            Context context,
            ConcurrentDictionary<string, ConcurrentBag<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>> xrefsByUid,
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

                    var (fileMetaErrors, fileMetadata) = context.MetadataProvider.GetFileMetadata(file, yamlHeader);
                    errors.AddRange(yamlHeaderErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        TryAddXref(xrefsByUid, fileMetadata.Uid, file, () =>
                        {
                            var (error, spec, _) = LoadMarkdown(context, fileMetadata, file);
                            return (error is null ? new List<Error>() : new List<Error> { error }, spec);
                        });
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Deserialize(file, context);
                    errors.AddRange(yamlErrors);
                    var obj = token as JObject;
                    var uid = obj?.Value<string>("uid");
                    if (!string.IsNullOrEmpty(uid))
                    {
                        TryAddXref(xrefsByUid, uid, file, () => LoadSchemaDocument(context, obj, file, uid));
                    }
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Deserialize(file, context);
                    errors.AddRange(jsonErrors);
                    var obj = token as JObject;
                    var uid = obj.Value<string>("uid");
                    if (!string.IsNullOrEmpty(uid))
                    {
                        TryAddXref(xrefsByUid, uid, file, () => LoadSchemaDocument(context, obj, file, uid));
                    }
                }
                context.Report.Write(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(file.ToString(), dex.Error);
            }
        }

        private static (Error error, InternalXrefSpec spec, Document doc) LoadMarkdown(Context context, FileMetadata metadata, Document file)
        {
            var xref = new InternalXrefSpec
            {
                Uid = metadata.Uid,
                Href = file.ExternalUrl,
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

        private static (List<Error> errors, InternalXrefSpec spec) LoadSchemaDocument(Context context, JObject obj, Document file, string uid)
        {
            var extensionData = new Dictionary<string, Lazy<JValue>>();

            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();
            var (schemaErrors, content) = JsonUtility.ToObjectWithSchemaValidation(
                obj,
                schema.Type,
                transform: AttributeTransformer.TransformXref(context, errors, file, null, extensionData));

            errors.AddRange(schemaErrors);
            var xref = new InternalXrefSpec
            {
                Uid = uid,
                Href = file.ExternalUrl,
                ReferencedFile = file,
            };

            xref.ExtensionData.AddRange(extensionData);
            return (errors, xref);
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>> xrefsByUid, string uid, Document file, Func<(List<Error>, InternalXrefSpec)> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<(Lazy<(List<Error>, InternalXrefSpec)>, Document)>()).Add((new Lazy<(List<Error>, InternalXrefSpec)>(func), file));
        }
    }
}
