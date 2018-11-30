// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec, Document)>>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;
        private readonly Context _context;
        private readonly MonikerComparer _monikerComparer;

        public IEnumerable<XrefSpec> InternalReferences
        {
            get
            {
                var loadedInternalSpecs = new List<XrefSpec>();
                foreach (var (uid, specsWithSameUid) in _internalXrefMap)
                {
                    if (TryGetValidXrefSpecs(uid, specsWithSameUid, out var validInternalSpecs, out var _))
                    {
                        loadedInternalSpecs.Add(GetLatestInternalXrefmap(validInternalSpecs));
                    }
                }
                return loadedInternalSpecs;
            }
        }

        public (XrefSpec, Document) Resolve(string uid, string moniker = null)
        {
            if (_internalXrefMap.TryGetValue(uid, out var internalSpecs))
            {
                if (!TryGetValidXrefSpecs(uid, internalSpecs, out var validInternalSpecs, out var referencedFile))
                    return default;

                if (!string.IsNullOrEmpty(moniker))
                {
                    foreach (var internalSpec in validInternalSpecs)
                    {
                        if (internalSpec.Monikers.Contains(moniker))
                        {
                            return (internalSpec, referencedFile);
                        }
                    }

                    // if the moniker is not defined with the uid
                    // log a warning and take the one with latest version
                    _context.Report(Errors.InvalidUidMoniker(moniker, uid));
                    return (GetLatestInternalXrefmap(validInternalSpecs), referencedFile);
                }

                // For uid with and without moniker range, take the one without moniker range
                var uidWithoutMoniker = validInternalSpecs.SingleOrDefault(spec => spec.Monikers.Count == 0);
                if (uidWithoutMoniker != null)
                {
                    return (uidWithoutMoniker, referencedFile);
                }

                // For uid with moniker range, take the latest moniker if no moniker defined while resolving
                if (internalSpecs.Count > 1)
                {
                    var latestInternalXrefMap = GetLatestInternalXrefmap(validInternalSpecs);
                    return (latestInternalXrefMap, referencedFile);
                }
                else
                {
                    var validInternalSpec = validInternalSpecs.Single();
                    return (validInternalSpec, referencedFile);
                }
            }

            if (_externalXrefMap.TryGetValue(uid, out var externalSpec))
            {
                return (externalSpec, null);
            }
            return default;
        }

        public static XrefMap Create(Context context, Docset docset)
        {
            Dictionary<string, XrefSpec> map = new Dictionary<string, XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var json = File.ReadAllText(docset.GetFileRestorePath(url));
                var xRefMap = JsonUtility.Deserialize<XrefMapModel>(json);
                foreach (var spec in xRefMap.References)
                {
                    map[spec.Uid] = spec;
                }
            }
            return new XrefMap(map, CreateInternalXrefMap(context, docset.ScanScope), context, docset.Monikers.Comparer);
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(InternalReferences);
            context.WriteJson(models, "xrefmap.json");
        }

        private XrefSpec GetLatestInternalXrefmap(List<XrefSpec> specs)
            => specs.OrderByDescending(item => item.Monikers.FirstOrDefault(), _monikerComparer).FirstOrDefault();

        private bool TryGetValidXrefSpecs(string uid, List<Lazy<(List<Error> errors, XrefSpec spec, Document doc)>> specsWithSameUid, out List<XrefSpec> validSpecs, out Document file)
        {
            var loadedSpecs = specsWithSameUid.Select(item => LoadXrefSpec(item));
            validSpecs = new List<XrefSpec>();
            file = null;

            // no conflicts
            if (loadedSpecs.Count() == 1)
            {
                validSpecs.AddRange(loadedSpecs.Select(x => x.Item1));
                file = loadedSpecs.Single().Item2;
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = loadedSpecs.Where(item => item.Item1.Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.Item1.Href);
                _context.Report(Errors.UidConflict(uid, orderedConflict.Select(x => x.Item1)));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single().Item1);
                file = conflictsWithoutMoniker.Single().Item2;
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => LoadXrefSpec(x).Item1.Monikers.Count > 0).Select(item => LoadXrefSpec(item));
            if (CheckOverlappingMonikers(loadedSpecs.Select(x => x.Item1), out var overlappingMonikers))
            {
                _context.Report(Errors.MonikerOverlapping(overlappingMonikers));
                return false;
            }

            // define same uid with non-overlapping monikers, add them all
            else
            {
                validSpecs.AddRange(conflictsWithMoniker.Select(x => x.Item1));
                return true;
            }

            (XrefSpec, Document) LoadXrefSpec(Lazy<(List<Error>, XrefSpec, Document)> value)
            {
                if (value is null)
                    return default;

                var isValueCreated = value.IsValueCreated;
                var (errors, spec, doc) = value.Value;
                if (!isValueCreated)
                {
                    foreach (var error in errors)
                    {
                        _context.Report(error);
                    }

                    // Sort monikers descending by moniker definition order
                    if (spec.Monikers.Count > 1)
                    {
                        var orderedMonikers = spec.Monikers.OrderBy(item => item, _monikerComparer).ToHashSet();
                        spec.Monikers = orderedMonikers;
                    }
                }
                return (spec, doc);
            }
        }

        private bool CheckOverlappingMonikers(IEnumerable<XrefSpec> specsWithSameUid, out HashSet<string> overlappingMonikers)
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

        private static IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec, Document)>>> CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec, Document)>>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                return xrefsByUid.ToList().OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value.ToList());
            }
        }

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec, Document)>>> internalXrefMap, Context context, MonikerComparer monikerComparer)
        {
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
            _context = context;
            _monikerComparer = monikerComparer;
        }

        private static void Load(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec, Document)>>> xrefsByUid, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
                    var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(file.Docset.Metadata.GetMetadata(file, yamlHeader));

                    errors.AddRange(yamlHeaderErrors);
                    if (!string.IsNullOrEmpty(metadata.Uid))
                    {
                        TryAddXref(xrefsByUid, metadata.Uid, () =>
                        {
                            var (error, spec, _) = LoadMarkdown(metadata, file);
                            return (error is null ? new List<Error>() : new List<Error> { error }, spec, file);
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
                        TryAddXref(xrefsByUid, uid, () => LoadSchemaDocument(obj, file, uid));
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
                        TryAddXref(xrefsByUid, uid, () => LoadSchemaDocument(obj, file, uid));
                    }
                }
                context.Report(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(file.ToString(), dex.Error);
            }
        }

        private static (Error error, XrefSpec spec, Document doc) LoadMarkdown(FileMetadata metadata, Document file)
        {
            var xref = new XrefSpec
            {
                Uid = metadata.Uid,
                Href = file.SiteUrl,
            };
            xref.ExtensionData["name"] = string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title;

            var (error, monikers) = file.Docset.Monikers.GetFileLevelMonikers(file, metadata.MonikerRange);
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref, file);
        }

        private static (List<Error> errors, XrefSpec spec, Document doc) LoadSchemaDocument(JObject obj, Document file, string uid)
        {
            var extensionData = new JObject();

            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();
            var (schemaErrors, content) = JsonUtility.ToObjectWithSchemaValidation(obj, schema.Type, transform: AttributeTransformer.Transform(errors, file, null, extensionData));
            errors.AddRange(schemaErrors);
            var xref = new XrefSpec
            {
                Uid = uid,
                Href = file.SiteUrl,
            };
            xref.ExtensionData.Merge(extensionData);
            return (errors, xref, file);
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec, Document)>>> xrefsByUid, string uid, Func<(List<Error>, XrefSpec, Document)> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<Lazy<(List<Error>, XrefSpec, Document)>>()).Add(new Lazy<(List<Error>, XrefSpec, Document)>(func));
        }
    }
}
