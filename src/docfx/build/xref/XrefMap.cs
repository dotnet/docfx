// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, XrefSpec> _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> map, IReadOnlyDictionary<string, XrefSpec> internalXrefMap)
        {
            _externalXrefMap = map;
            _internalXrefMap = internalXrefMap;
        }

        public XrefSpec Resolve(string uid)
        {
            if (_internalXrefMap != null && _internalXrefMap.TryGetValue(uid, out var xrefSpec))
            {
                return xrefSpec;
            }
            if (_externalXrefMap.TryGetValue(uid, out xrefSpec))
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
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xRefMap) = JsonUtility.Deserialize<XrefMapModel>(json);
                foreach (var sepc in xRefMap.References)
                {
                    map[sepc.Uid] = sepc;
                }
            }
            return new XrefMap(map, docset.Config.BuildInternalXrefMap ? CreateInternalXrefMap(context, docset.BuildScope) : null);
        }

        public static IReadOnlyDictionary<string, XrefSpec> CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<XrefSpec>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                var xrefs = HandleXrefConflicts(context, xrefsByUid);
                return xrefs;
            }
        }

        private static void Load(Context context, ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefsByUid, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    TryAddXref(xrefsByUid, LoadMarkdown(content, file));
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Deserialize(content);
                    errors.AddRange(yamlErrors);
                    TryAddXref(xrefsByUid, LoadSchemaDocument(errors, token, file));
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Deserialize(content);
                    errors.AddRange(jsonErrors);
                    TryAddXref(xrefsByUid, LoadSchemaDocument(errors, token, file));
                }
                context.Report(file.ToString(), errors);
            }
            catch (DocfxException ex)
            {
                context.Report(file.ToString(), ex.Error);
            }
        }

        private static Dictionary<string, XrefSpec> HandleXrefConflicts(Context context, ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefsByUid)
        {
            var result = new Dictionary<string, XrefSpec>();
            foreach (var (uid, conflict) in xrefsByUid)
            {
                if (conflict.Count > 1)
                {
                    var orderedConflict = conflict.OrderBy(spec => spec.Href);
                    context.Report(Errors.UidConflict(uid, orderedConflict));
                    result.Add(uid, orderedConflict.First());
                    continue;
                }
                result.Add(uid, conflict.First());
            }
            return result;
        }

        private static XrefSpec LoadMarkdown(string content, Document file)
        {
            var (_, markup) = Markup.ToHtml(content, file, null, null, null, null, MarkdownPipelineType.ConceptualMarkdown);
            var uid = markup.Metadata.Value<string>("uid");
            var title = markup.Metadata.Value<string>("title");
            if (!string.IsNullOrEmpty(uid))
            {
                var xref = new XrefSpec
                {
                    Uid = uid,
                    Href = file.SitePath,
                };
                xref.ExtensionData["name"] = string.IsNullOrEmpty(title) ? uid : title;
                return xref;
            }
            return null;
        }

        private static XrefSpec LoadSchemaDocument(List<Error> errors, JToken token, Document file)
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

            var (schemaErrors, content) = JsonUtility.ToObject(token, schema.Type, transform: TransformContent);
            errors.AddRange(schemaErrors);
            if (!string.IsNullOrEmpty(obj.Value<string>("uid")))
            {
                var xref = new XrefSpec
                {
                    Uid = obj.Value<string>("uid"),
                    Href = file.SitePath,
                };
                xref.ExtensionData.Merge(extensionData);
                return xref;
            }
            else if (extensionData.Count > 0)
            {
                errors.Add(Errors.UidMissing());
            }
            return null;

            object TransformContent(DataTypeAttribute attribute, object value, string jsonPath, string fieldName)
            {
                if (attribute is XrefPropertyAttribute)
                {
                    extensionData[jsonPath] = (string)value;
                }
                return (string)value;
            }
        }

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefConflicts, XrefSpec spec)
        {
            if (spec is null)
            {
                return;
            }
            xrefConflicts.GetOrAdd(spec.Uid, _ => new ConcurrentBag<XrefSpec>()).Add(spec);
        }
    }
}
