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
        private readonly IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec)>>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;
        private readonly Context _context;
        private readonly MonikerRangeParser _parser;

        public IEnumerable<XrefSpec> InternalReferences
            => _internalXrefMap
            .ToDictionary(item => item.Key, item => item.Value.OrderBy(x => LoadXrefSpec(x, _context, _parser).Monikers, new MonikerHashsetDesccendingComparer(_parser)).FirstOrDefault())
            .Values.Select(x => LoadXrefSpec(x, _context, _parser));

        public XrefSpec Resolve(string uid, string moniker = null)
        {
            if (_internalXrefMap.TryGetValue(uid, out var internalSpecs))
            {
                if (!string.IsNullOrEmpty(moniker))
                {
                    foreach (var internalSpec in internalSpecs)
                    {
                        var spec = LoadXrefSpec(internalSpec, _context, _parser);
                        if (spec.Monikers.Contains(moniker))
                        {
                            return spec;
                        }
                    }

                    // if the moniker is not defined with the uid
                    // log a warning and take the one with latest version
                    _context.Report(Errors.InvalidUidMoniker(moniker, uid));
                    return LoadXrefSpec(GetLatestInternalXrefmap(internalSpecs), _context, _parser);
                }

                // For uid with and without moniker range, take the one without moniker range
                var uidWithoutMoniker = internalSpecs.SingleOrDefault(spec => LoadXrefSpec(spec, _context, _parser).Monikers.Count == 0);
                if (uidWithoutMoniker != null)
                {
                    return LoadXrefSpec(uidWithoutMoniker, _context, _parser);
                }

                // For uid with moniker range, take the latest moniker if no moniker defined while resolving
                if (internalSpecs.Count > 1)
                {
                    return LoadXrefSpec(GetLatestInternalXrefmap(internalSpecs), _context, _parser);
                }
                else
                {
                    return LoadXrefSpec(internalSpecs.Single(), _context, _parser);
                }
            }
            if (_externalXrefMap.TryGetValue(uid, out var externalSpec))
            {
                return externalSpec;
            }
            return null;
        }

        public static XrefMap Create(Context context, Docset docset)
        {
            Dictionary<string, XrefSpec> map = new Dictionary<string, XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var json = File.ReadAllText(docset.RestoreMap.GetFileRestorePath(url));
                var xRefMap = JsonUtility.Deserialize<XrefMapModel>(json);
                foreach (var spec in xRefMap.References)
                {
                    map[spec.Uid] = spec;
                }
            }
            return new XrefMap(map, CreateInternalXrefMap(context, docset.MonikerRangeParser, docset.ScanScope), context, docset.MonikerRangeParser);
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(InternalReferences);
            context.WriteJson(models, "xrefmap.json");
        }

        private Lazy<(List<Error>, XrefSpec)> GetLatestInternalXrefmap(List<Lazy<(List<Error>, XrefSpec)>> specs)
            => specs.OrderBy(item => item.Value.Item2.Monikers, new MonikerHashsetDesccendingComparer(_parser)).FirstOrDefault();

        private static IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec)>>> CreateInternalXrefMap(Context context, MonikerRangeParser parser, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                var xrefs = HandleXrefConflicts(context, parser, xrefsByUid);
                return xrefs;
            }
        }

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, List<Lazy<(List<Error>, XrefSpec)>>> internalXrefMap, Context context, MonikerRangeParser parser)
        {
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
            _context = context;
            _parser = parser;
        }

        private static void Load(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>> xrefsByUid, Document file)
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
                            var (error, spec) = LoadMarkdown(metadata, file);
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

        private static XrefSpec LoadXrefSpec(Lazy<(List<Error>, XrefSpec)> value, Context context, MonikerRangeParser parser)
        {
            if (value is null)
                return null;

            var isValueCreated = value.IsValueCreated;
            var (errors, spec) = value.Value;
            if (!isValueCreated)
            {
                foreach (var error in errors)
                {
                    context.Report(error);
                }

                // Sort monikers descending by moniker definition order
                if (spec.Monikers.Count > 1)
                {
                    var orderedMonikers = spec.Monikers.OrderBy(item => item, new MonikerDescendingComparer(parser)).ToHashSet();
                    spec.Monikers = orderedMonikers;
                }
            }
            return spec;
        }

        private static Dictionary<string, List<Lazy<(List<Error>, XrefSpec)>>> HandleXrefConflicts(Context context, MonikerRangeParser parser, ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>> xrefsByUid)
        {
            var result = new List<(string uid, List<Lazy<(List<Error>, XrefSpec)>> specs)>();
            foreach (var (uid, specsWithSameUid) in xrefsByUid)
            {
                if (TryGetXrefSpecs(uid, specsWithSameUid, context, parser, out var validSpecs))
                {
                    result.Add((uid, validSpecs));
                }
            }
            return result.OrderBy(item => item.uid).ToDictionary(item => item.uid, item => item.specs);
        }

        private static bool TryGetXrefSpecs(string uid, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>> specsWithSameUid, Context context, MonikerRangeParser parser, out List<Lazy<(List<Error>, XrefSpec)>> validSpecs)
        {
            validSpecs = new List<Lazy<(List<Error>, XrefSpec)>>();

            // no conflicts
            if (specsWithSameUid.Count == 1)
            {
                validSpecs.AddRange(specsWithSameUid);
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = specsWithSameUid.Where(x => LoadXrefSpec(x, context, parser).Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => LoadXrefSpec(item, context, parser)?.Href);
                context.Report(Errors.UidConflict(uid, orderedConflict.Select(v => LoadXrefSpec(v, context, parser))));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single());
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => LoadXrefSpec(x, context, parser).Monikers.Count > 0);
            if (CheckOverlappingMonikers(specsWithSameUid, context, parser, out var overlappingMonikers))
            {
                context.Report(Errors.MonikerOverlapping(overlappingMonikers));
                return false;
            }

            // define same uid with non-overlapping monikers, add them all
            else
            {
                validSpecs.AddRange(conflictsWithMoniker);
                return true;
            }
        }

        private static bool CheckOverlappingMonikers(ConcurrentBag<Lazy<(List<Error>, XrefSpec)>> specsWithSameUid, Context context, MonikerRangeParser parser, out HashSet<string> overlappingMonikers)
        {
            bool isOverlapping = false;
            overlappingMonikers = new HashSet<string>();
            var monikerHashSet = new HashSet<string>();
            foreach (var spec in specsWithSameUid)
            {
                foreach (var moniker in LoadXrefSpec(spec, context, parser).Monikers)
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

        private static (Error, XrefSpec) LoadMarkdown(FileMetadata metadata, Document file)
        {
            var xref = new XrefSpec
            {
                Uid = metadata.Uid,
                Href = file.SiteUrl,
            };
            xref.ExtensionData["name"] = string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title;

            var (error, monikers) = file.Docset.MonikersProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref);
        }

        private static (List<Error> errors, XrefSpec spec) LoadSchemaDocument(JObject obj, Document file, string uid)
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
            return (errors, xref);
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>> xrefsByUid, string uid, Func<(List<Error>, XrefSpec)> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>()).Add(new Lazy<(List<Error>, XrefSpec)>(func));
        }

        private class MonikerDescendingComparer : IComparer<string>
        {
            private readonly MonikerRangeParser _parser;

            public MonikerDescendingComparer(MonikerRangeParser parser)
            {
                _parser = parser;
            }

            public int Compare(string x, string y)
            {
                if (x is null)
                    return 1;
                if (y is null)
                    return -1;
                return _parser.GetMonikerOrderFromDefinition(y).CompareTo(_parser.GetMonikerOrderFromDefinition(x));
            }
        }

        private class MonikerHashsetDesccendingComparer : IComparer<HashSet<string>>
        {
            private readonly MonikerRangeParser _parser;

            public MonikerHashsetDesccendingComparer(MonikerRangeParser parser)
            {
                _parser = parser;
            }

            public int Compare(HashSet<string> x, HashSet<string> y)
            {
                if (x.FirstOrDefault() is null)
                    return 1;
                if (y.FirstOrDefault() is null)
                    return -1;
                return _parser.GetMonikerOrderFromDefinition(y.First()).CompareTo(_parser.GetMonikerOrderFromDefinition(x.First()));
            }
        }
    }
}
