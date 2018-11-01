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
        private readonly IReadOnlyDictionary<string, Lazy<(List<Error>, XrefSpec)>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;
        private readonly Context _context;

        public IEnumerable<XrefSpec> InternalReferences
            => _internalXrefMap.Values.Select(v => LoadXrefSpec(v, _context));

        public XrefSpec Resolve(string uid)
        {
            if (_internalXrefMap.TryGetValue(uid, out var internalSpec))
            {
                return LoadXrefSpec(internalSpec, _context);
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
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(url));
                var (_, xRefMap) = JsonUtility.Deserialize<XrefMapModel>(json);
                foreach (var sepc in xRefMap.References)
                {
                    map[sepc.Uid] = sepc;
                }
            }
            return new XrefMap(map, CreateInternalXrefMap(context, docset.ScanScope));
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(InternalReferences);
            context.WriteJson(models, "xrefmap.json");
        }

        private static IReadOnlyDictionary<string, Lazy<(List<Error>, XrefSpec)>> CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                var xrefs = HandleXrefConflicts(context, xrefsByUid);
                return xrefs;
            }
        }

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, Lazy<(List<Error>, XrefSpec)>> internalXrefMap, Context context)
        {
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
            _context = context;
        }

        private static void Load(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>> xrefsByUid, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (metaErrors, metadata) = ExtractYamlHeader.Extract(file, context);
                    errors.AddRange(metaErrors);
                    var uid = metadata.Value<string>("uid");
                    if (!string.IsNullOrEmpty(uid))
                    {
                        TryAddXref(xrefsByUid, uid, () => (new List<Error>(), LoadMarkdown(uid, metadata, file)));
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Deserialize(file, context);
                    errors.AddRange(yamlErrors);

                    var obj = token as JObject;
                    var uid = obj.Value<string>("uid");
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

        private static XrefSpec LoadXrefSpec(Lazy<(List<Error>, XrefSpec)> value, Context context)
        {
            if (value is null)
                return null;

            var (errors, spec) = value.Value;
            foreach (var error in errors)
            {
                context.Report(error);
            }
            return spec;
        }

        private static Dictionary<string, Lazy<(List<Error>, XrefSpec)>> HandleXrefConflicts(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<(List<Error>, XrefSpec)>>> xrefsByUid)
        {
            var result = new List<(string, Lazy<(List<Error>, XrefSpec)>)>();
            foreach (var (uid, conflict) in xrefsByUid)
            {
                if (conflict.Count > 1)
                {
                    var orderedConflict = conflict.OrderBy(item => LoadXrefSpec(item, context)?.Href);
                    context.Report(Errors.UidConflict(uid, orderedConflict.Select(v => v.Value.Item2)));
                    result.Add((uid, orderedConflict.First()));
                    continue;
                }
                result.Add((uid, conflict.First()));
            }
            return result.OrderBy(item => item.Item1).ToDictionary(item => item.Item1, item => item.Item2);
        }

        private static XrefSpec LoadMarkdown(string uid, JObject metadata, Document file)
        {
            var title = metadata.Value<string>("title");
            var xref = new XrefSpec
            {
                Uid = uid,
                Href = file.SitePath,
            };
            xref.ExtensionData["name"] = string.IsNullOrEmpty(title) ? uid : title;
            return xref;
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
            var (schemaErrors, content) = JsonUtility.ToObject(obj, schema.Type, transform: AttributeTransformer.Transform(errors, file, null, extensionData));
            errors.AddRange(schemaErrors);
            var xref = new XrefSpec
            {
                Uid = uid,
                Href = file.SitePath,
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
    }
}
