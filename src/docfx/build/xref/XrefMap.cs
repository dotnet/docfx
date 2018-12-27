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
        private readonly IReadOnlyDictionary<string, List<(XrefSpec specs, Document file, List<Document> callStack)>> _internalXrefMap;
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
                    if (TryGetValidXrefSpecs(uid, null, specsWithSameUid, out var validInternalSpecs))
                    {
                        loadedInternalSpecs.Add(GetLatestInternalXrefMap(validInternalSpecs).spec);
                    }
                }
                return loadedInternalSpecs;
            }
        }

        public (XrefSpec spec, Document referencedFile, List<Document> callStack) Resolve(string uid, Document file, string moniker = null)
        {
            if (_internalXrefMap.TryGetValue(uid, out var internalSpecs))
            {
                return GetInternalSpec(uid, file, moniker, internalSpecs);
            }

            if (_externalXrefMap.TryGetValue(uid, out var externalSpec))
            {
                return (externalSpec, null, new List<Document>());
            }
            return (null, null, new List<Document>());
        }

        private (XrefSpec internalSpec, Document referencedFile, List<Document> callStack) GetInternalSpec(string uid, Document file, string moniker, List<(XrefSpec, Document, List<Document>)> internalSpecs)
        {
            if (!TryGetValidXrefSpecs(uid, file, internalSpecs, out var validInternalSpecs))
                return default;

            if (!string.IsNullOrEmpty(moniker))
            {
                foreach (var (internalSpec, doc, files) in validInternalSpecs)
                {
                    if (internalSpec.Monikers.Contains(moniker))
                    {
                        return (internalSpec, doc, files);
                    }
                }

                // if the moniker is not defined with the uid
                // log a warning and take the one with latest version
                _context.Report(Errors.InvalidUidMoniker(moniker, uid));
                return GetLatestInternalXrefMap(validInternalSpecs);
            }

            // For uid with and without moniker range, take the one without moniker range
            var (uidWithoutMoniker, referencedFile, callStack) = validInternalSpecs.SingleOrDefault(item => item.spec?.Monikers.Count == 0);
            if (uidWithoutMoniker != null)
            {
                return (uidWithoutMoniker, referencedFile, callStack);
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

        public static XrefMap Create(
            Context context,
            Docset docset,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            DependencyResolver dependencyResolver)
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
            return new XrefMap(map, CreateInternalXrefMap(context, docset.ScanScope, metadataProvider, monikerProvider, dependencyResolver), context, monikerProvider.Comparer);
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(InternalReferences);
            context.WriteJson(models, "xrefmap.json");
        }

        private (XrefSpec spec, Document referencedFile, List<Document> callStack) GetLatestInternalXrefMap(List<(XrefSpec spec, Document referencedFile, List<Document> callStack)> specs)
            => specs.OrderByDescending(item => item.spec.Monikers.FirstOrDefault(), _monikerComparer).FirstOrDefault();

        private bool TryGetValidXrefSpecs(string uid, Document rootFile, List<(XrefSpec spec, Document file, List<Document> callStack)> specsWithSameUid, out List<(XrefSpec spec, Document file, List<Document>)> validSpecs)
        {
            validSpecs = new List<(XrefSpec, Document, List<Document>)>();

            // no conflicts
            if (specsWithSameUid.Count() == 1)
            {
                validSpecs.AddRange(specsWithSameUid);
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = specsWithSameUid.Where(item => item.Item1.Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.Item1.Href);
                _context.Report(Errors.UidConflict(uid, orderedConflict.Select(x => x.Item1)));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single());
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Item1?.Monikers.Count > 0);
            if (CheckOverlappingMonikers(specsWithSameUid.Select(x => x.Item1), out var overlappingMonikers))
            {
                _context.Report(Errors.MonikerOverlapping(overlappingMonikers));
                return false;
            }

            // define same uid with non-overlapping monikers, add them all
            else
            {
                validSpecs.AddRange(conflictsWithMoniker);
                return true;
            }

            //(XrefSpec, Document, List<Document>) LoadXrefSpec((Lazy<(List<Error>, XrefSpec)> value, Document file, List<Document> callStack) input)
            //{
            //    if (input.value is null)
            //        return default;

            //    //if (input.file == rootFile)
            //    //{
            //    //    throw Errors.CircularReference().ToException();
            //    //}

            //    var isValueCreated = input.value.IsValueCreated;

            //    var (errors, spec) = input.value.Value;
            //    if (!isValueCreated)
            //    {
            //        foreach (var error in errors)
            //        {
            //            _context.Report(error);
            //        }

            //        // Sort monikers descending by moniker definition order
            //        if (spec.Monikers.Count > 1)
            //        {
            //            var orderedMonikers = spec.Monikers.OrderBy(item => item, _monikerComparer).ToHashSet();
            //            spec.Monikers = orderedMonikers;
            //        }
            //    }
            //    return (spec, input.file, input.callStack);
            //}
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

        private static IReadOnlyDictionary<string, List<(XrefSpec, Document, List<Document>)>>
            CreateInternalXrefMap(Context context, IEnumerable<Document> files, MetadataProvider metadataProvider, MonikerProvider monikerProvider, DependencyResolver dependencyResolver)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<(XrefSpec, Document, List<Document>)>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file, metadataProvider, monikerProvider, dependencyResolver), Progress.Update);
                return xrefsByUid.ToList().OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value.ToList());
            }
        }

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, List<(XrefSpec, Document, List<Document>)>> internalXrefMap, Context context, MonikerComparer monikerComparer)
        {
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
            _context = context;
            _monikerComparer = monikerComparer;
        }

        private static void Load(
            Context context,
            ConcurrentDictionary<string, ConcurrentBag<(XrefSpec, Document, List<Document>)>> xrefsByUid,
            Document file,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            DependencyResolver dependencyResolver)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                var callStack = new List<Document> { file };
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);

                    var (fileMetaErrors, fileMetadata) = metadataProvider.GetFileMetadata(file, yamlHeader);
                    errors.AddRange(yamlHeaderErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        var (markdownError, spec) = LoadMarkdown(fileMetadata, file, monikerProvider);
                        errors.AddIfNotNull(markdownError);
                        TryAddXref(xrefsByUid, fileMetadata.Uid, file, callStack, spec);
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
                        var (specErrors, spec) = LoadSchemaDocument(obj, file, uid, dependencyResolver, callStack);
                        errors.AddRange(specErrors);
                        TryAddXref(xrefsByUid, uid, file, callStack, spec);
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
                        var (specErrors, spec) = LoadSchemaDocument(obj, file, uid, dependencyResolver, callStack);
                        TryAddXref(xrefsByUid, uid, file, callStack, spec);
                    }
                }
                context.Report(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(file.ToString(), dex.Error);
            }
        }

        private static (Error error, XrefSpec spec) LoadMarkdown(FileMetadata metadata, Document file, MonikerProvider monikerProvider)
        {
            var xref = new XrefSpec
            {
                Uid = metadata.Uid,
                Href = file.SiteUrl,
            };
            xref.ExtensionData["name"] = new Lazy<object>(() => string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title);

            var (error, monikers) = monikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref);
        }

        private static (List<Error> errors, XrefSpec spec) LoadSchemaDocument(JObject obj, Document file, string uid, DependencyResolver dependencyResolver, List<Document> callStack)
        {
            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();
            var result = new Lazy<(List<Error>, object)>(() => JsonUtility.ToObjectWithSchemaValidation(
                obj,
                schema.Type,
                transform: AttributeTransformer.Transform(errors, file, dependencyResolver, null, callStack)));

            //errors.AddRange(schemaErrors);
            var xref = new XrefSpec
            {
                Uid = uid,
                Href = file.SiteUrl,
            };

            foreach (var  in result)
            {
                xref.ExtensionData.TryAdd(key, value);
            }
            return (errors, xref);
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<(XrefSpec, Document, List<Document>)>> xrefsByUid, string uid, Document file, List<Document> callStack, XrefSpec spec)
            => xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<(XrefSpec, Document, List<Document>)>()).Add((spec, file, callStack));
    }
}
