// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefMap
    {
        private readonly IReadOnlyDictionary<string, XrefSpec> _map;

        private InternalXrefMap(IReadOnlyDictionary<string, XrefSpec> map)
        {
            _map = map;
        }

        public static async Task<InternalXrefMap> Create(Context context, IEnumerable<Document> files)
        {
            ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefConflicts = new ConcurrentDictionary<string, ConcurrentBag<XrefSpec>>();
            ConcurrentDictionary<string, XrefSpec> xrefs = new ConcurrentDictionary<string, XrefSpec>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                await ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefs, xrefConflicts, file), Progress.Update);
                HandleXrefConflicts(context, xrefs, xrefConflicts);
            }
            return new InternalXrefMap(xrefs);
        }

        public bool Resolve(string uid, out XrefSpec xref) => _map.TryGetValue(uid, out xref);

        private static Task Load(Context context, ConcurrentDictionary<string, XrefSpec> xrefs, ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefConflicts, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    TryAddXref(xrefs, xrefConflicts, LoadMarkdown(content, file));
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Deserialize(content);
                    errors.AddRange(yamlErrors);
                    TryAddXref(xrefs, xrefConflicts, LoadSchemaDocument(errors, token, file));
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Deserialize(content);
                    errors.AddRange(jsonErrors);
                    TryAddXref(xrefs, xrefConflicts, LoadSchemaDocument(errors, token, file));
                }
                context.Report(file.ToString(), errors);
                return Task.CompletedTask;
            }
            catch (DocfxException ex)
            {
                context.Report(file.ToString(), ex.Error);
                return Task.CompletedTask;
            }
        }

        private static void HandleXrefConflicts(Context context, ConcurrentDictionary<string, XrefSpec> xrefs, ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefConflicts)
        {
            foreach (var (uid, conflict) in xrefConflicts)
            {
                var conflictingSpecs = new HashSet<XrefSpec>();
                foreach (var xref in conflict)
                {
                    conflictingSpecs.Add(xref);
                }

                if (xrefs.TryRemove(uid, out var removed))
                {
                    conflictingSpecs.Add(removed);
                }
                context.Report(Errors.UidConflict(uid, conflictingSpecs));
            }
        }

        private static XrefSpec LoadMarkdown(string content, Document file)
        {
            var (_, markup) = Markup.ToHtml(content, file, null, null, null, null, MarkdownPipelineType.ConceptualMarkdown);

            var uid = markup.Metadata.Value<string>("uid");
            var title = markup.Metadata.Value<string>("title");
            if (!string.IsNullOrEmpty(uid))
            {
                return new XrefSpec
                {
                    Uid = uid,
                    Name = string.IsNullOrEmpty(title) ? uid : title,
                    Href = file.SitePath,
                };
            }
            return null;
        }

        private static XrefSpec LoadSchemaDocument(List<Error> errors, JToken token, Document file)
        {
            XrefSpec xref = null;

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
            return xref;

            object TransformContent(DataTypeAttribute attribute, object value)
            {
                if (attribute is UidAttribute)
                {
                    if (!string.IsNullOrEmpty((string)value))
                    {
                        // TODO: set name and extension data based on defined xref properties in the schema type
                        xref = new XrefSpec { Uid = (string)value, Href = file.FilePath };
                    }
                }
                return (string)value;
            }
        }

        private static void TryAddXref(ConcurrentDictionary<string, XrefSpec> xrefs, ConcurrentDictionary<string, ConcurrentBag<XrefSpec>> xrefConflicts, XrefSpec spec)
        {
            if (spec is null)
            {
                return;
            }

            if (!xrefs.TryAdd(spec.Uid, spec))
            {
                if (xrefs.TryGetValue(spec.Uid, out var existingXref))
                {
                    xrefConflicts.GetOrAdd(spec.Uid, _ => new ConcurrentBag<XrefSpec>()).Add(spec);
                }
            }
        }
    }
}
