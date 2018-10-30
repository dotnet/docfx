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
        private readonly IReadOnlyDictionary<string, Lazy<XrefSpec>> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;

        public IEnumerable<XrefSpec> InternalReferences => _internalXrefMap.Values.Select(v => v.Value);

        public XrefSpec Resolve(string uid)
        {
            if (_internalXrefMap.TryGetValue(uid, out var sepc))
            {
                return sepc?.Value;
            }
            if (_externalXrefMap.TryGetValue(uid, out var xrefSpec))
            {
                return xrefSpec;
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
            return new XrefMap(map, docset.Config.BuildInternalXrefMap ? CreateInternalXrefMap(context, docset.ScanScope) : new Dictionary<string, Lazy<XrefSpec>>());
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(InternalReferences);
            context.WriteJson(models, "xrefmap.json");
        }

        private static IReadOnlyDictionary<string, Lazy<XrefSpec>> CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            ConcurrentDictionary<string, ConcurrentBag<Lazy<XrefSpec>>> xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<Lazy<XrefSpec>>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                var xrefs = HandleXrefConflicts(context, xrefsByUid);
                return xrefs;
            }
        }

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> externalXrefMap, IReadOnlyDictionary<string, Lazy<XrefSpec>> internalXrefMap)
        {
            _externalXrefMap = externalXrefMap;
            _internalXrefMap = internalXrefMap;
        }

        private static void Load(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<XrefSpec>>> xrefsByUid, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (metaErrors, metadata) = ExtractYamlHeader.Extract(file, context);
                    errors.AddRange(metaErrors);
                    var (uid, spec) = LoadMarkdown(metadata, file);
                    TryAddXref(xrefsByUid, uid, spec);
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Deserialize(file, context);
                    errors.AddRange(yamlErrors);
                    var (uid, spec) = LoadSchemaDocument(errors, token, file);
                    TryAddXref(xrefsByUid, uid, spec);
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Deserialize(file, context);
                    errors.AddRange(jsonErrors);
                    var (uid, spec) = LoadSchemaDocument(errors, token, file);
                    TryAddXref(xrefsByUid, uid, spec);
                }
                context.Report(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(file.ToString(), dex.Error);
            }
        }

        private static SortedDictionary<string, Lazy<XrefSpec>> HandleXrefConflicts(Context context, ConcurrentDictionary<string, ConcurrentBag<Lazy<XrefSpec>>> xrefsByUid)
        {
            var result = new SortedDictionary<string, Lazy<XrefSpec>>();
            foreach (var (uid, conflict) in xrefsByUid)
            {
                if (conflict.Count > 1)
                {
                    var orderedConflict = conflict.OrderBy(spec => spec.Value.Href);
                    context.Report(Errors.UidConflict(uid, orderedConflict.Select(v => v.Value)));
                    result.Add(uid, orderedConflict.First());
                    continue;
                }
                result.Add(uid, conflict.First());
            }
            return result;
        }

        private static (string uid, Lazy<XrefSpec> spec) LoadMarkdown(JObject metadata, Document file)
        {
            var uid = metadata.Value<string>("uid");
            var title = metadata.Value<string>("title");
            if (!string.IsNullOrEmpty(uid))
            {
                return (uid, new Lazy<XrefSpec>(() =>
                {
                    var xref = new XrefSpec
                    {
                        Uid = uid,
                        Href = file.SitePath,
                    };
                    xref.ExtensionData["name"] = string.IsNullOrEmpty(title) ? uid : title;
                    return xref;
                }));
            }
            return default;
        }

        private static (string uid, Lazy<XrefSpec> spec) LoadSchemaDocument(List<Error> errors, JToken token, Document file)
        {
            var extensionData = new JObject();

            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var obj = token as JObject;
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var uid = obj.Value<string>("uid");
            if (!string.IsNullOrEmpty(uid))
            {
                return (uid, new Lazy<XrefSpec>(() =>
                {
                    var (schemaErrors, content) = JsonUtility.ToObject(token, schema.Type, transform: AttributeTransformer.Transform(errors, file, null, extensionData));
                    errors.AddRange(schemaErrors);
                    var xref = new XrefSpec
                    {
                        Uid = uid,
                        Href = file.SitePath,
                    };
                    xref.ExtensionData.Merge(extensionData);
                    return xref;
                }));
            }
            return default;
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<Lazy<XrefSpec>>> xrefsByUid, string uid, Lazy<XrefSpec> spec)
        {
            if (spec is null)
            {
                return;
            }
            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<Lazy<XrefSpec>>()).Add(spec);
        }
    }
}
