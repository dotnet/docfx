// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaTransformer
    {
        private readonly MarkdownEngine _markdownEngine;
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly ErrorLog _errorLog;

        private readonly ConcurrentDictionary<Document, int> _uidCountCache =
                     new ConcurrentDictionary<Document, int>(ReferenceEqualsComparer.Default);

        private static ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                 = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(MarkdownEngine markdownEngine, LinkResolver linkResolver, XrefResolver xrefResolver, ErrorLog errorLog)
        {
            _markdownEngine = markdownEngine;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _errorLog = errorLog;
        }

        public (List<Error> errors, JToken token) TransformContent(JsonSchema schema, Document file, JToken token)
        {
            var definitions = new JsonSchemaDefinition(schema);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(definitions, schema, token));
            return TransformContentCore(definitions, file, schema, token, uidCount);
        }

        public (List<Error>, IReadOnlyList<InternalXrefSpec>) LoadXrefSpecs(JsonSchema schema, Document file, JToken token)
        {
            var errors = new List<Error>();
            var xrefSpecs = new List<InternalXrefSpec>();
            var definitions = new JsonSchemaDefinition(schema);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(definitions, schema, token));
            LoadXrefSpecsCore(file, schema, definitions, token, errors, xrefSpecs, uidCount);
            return (errors, xrefSpecs);
        }

        private void LoadXrefSpecsCore(
            Document file,
            JsonSchema schema,
            JsonSchemaDefinition definitions,
            JToken node,
            List<Error> errors,
            List<InternalXrefSpec> xrefSpecs,
            int uidCount)
        {
            schema = definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out var uid))
                    {
                        xrefSpecs.Add(LoadXrefSpec(definitions, file, schema, uid, obj, uidCount));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            LoadXrefSpecsCore(file, propertySchema, definitions, value, errors, xrefSpecs, uidCount);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(file, schema.Items.schema, definitions, item, errors, xrefSpecs, uidCount);
                    }
                    break;
            }
        }

        private InternalXrefSpec LoadXrefSpec(
            JsonSchemaDefinition definitions,
            Document file,
            JsonSchema schema,
            SourceInfo<string> uid,
            JObject obj,
            int uidCount)
        {
            var href = GetXrefHref(file, uid, uidCount, obj.Parent == null);
            var xref = new InternalXrefSpec(uid, href, file);

            foreach (var xrefProperty in schema.XrefProperties)
            {
                if (!obj.TryGetValue(xrefProperty, out var value))
                {
                    xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => JValue.CreateNull());
                    continue;
                }

                if (!schema.Properties.TryGetValue(xrefProperty, out var propertySchema))
                {
                    xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => value);
                    continue;
                }

                xref.XrefProperties[xrefProperty] = new Lazy<JToken>(
                    () => LoadXrefProperty(definitions, file, uid, value, propertySchema, uidCount),
                    LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private int GetFileUidCount(JsonSchemaDefinition definitions, JsonSchema schema, JToken node)
        {
            schema = definitions.GetDefinition(schema);
            var count = 0;
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out _))
                    {
                        count++;
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            count += GetFileUidCount(definitions, propertySchema, value);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        count += GetFileUidCount(definitions, schema.Items.schema, item);
                    }
                    break;
            }
            return count;
        }

        private bool IsXrefSpec(JObject obj, JsonSchema schema, out SourceInfo<string> uid)
        {
            uid = default;

            // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
            if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string tempUid &&
                schema.Properties.TryGetValue("uid", out var uidSchema) && uidSchema.ContentType == JsonSchemaContentType.Uid)
            {
                uid = new SourceInfo<string>(tempUid, uidValue.GetSourceInfo());
                return true;
            }
            return false;
        }

        private string GetXrefHref(Document file, string uid, int uidCount, bool isRootLevel)
            => !isRootLevel && uidCount > 1 ? UrlUtility.MergeUrl(file.SiteUrl, "", $"#{Regex.Replace(uid, @"\W", "_")}") : file.SiteUrl;

        private JToken LoadXrefProperty(
            JsonSchemaDefinition definitions,
            Document file,
            SourceInfo<string> uid,
            JToken value,
            JsonSchema schema,
            int uidCount)
        {
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
            }

            try
            {
                recursionDetector.Push(uid);
                var (transformErrors, transformedToken) = TransformContentCore(definitions, file, schema, value, uidCount);
                _errorLog.Write(transformErrors);
                return transformedToken;
            }
            finally
            {
                Debug.Assert(recursionDetector.Count > 0);
                recursionDetector.Pop();
            }
        }

        private (List<Error>, JToken) TransformContentCore(JsonSchemaDefinition definitions, Document file, JsonSchema schema, JToken token, int uidCount)
        {
            var errors = new List<Error>();
            schema = definitions.GetDefinition(schema);
            switch (token)
            {
                // transform array and object is not supported yet
                case JArray array:
                    if (schema.Items.schema is null)
                    {
                        return (errors, array);
                    }

                    var newArray = new JArray();
                    foreach (var item in array)
                    {
                        var (arrayErrors, newItem) = TransformContentCore(definitions, file, schema.Items.schema, item, uidCount);
                        errors.AddRange(arrayErrors);
                        newArray.Add(newItem);
                    }

                    return (errors, newArray);

                case JObject obj:
                    var newObject = new JObject();
                    foreach (var (key, value) in obj)
                    {
                        if (value is null)
                        {
                            continue;
                        }
                        else if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            var (propertyErrors, transformedValue) = TransformContentCore(definitions, file, propertySchema, value, uidCount);
                            errors.AddRange(propertyErrors);
                            newObject[key] = transformedValue;
                        }
                        else
                        {
                            newObject[key] = value;
                        }
                    }
                    if (IsXrefSpec(obj, schema, out var uid))
                    {
                        if (obj.TryGetValue("href", out var href))
                        {
                            errors.Add(Errors.Metadata.AttributeReserved(href.GetSourceInfo()?.KeySourceInfo!, "href"));
                        }
                        newObject["href"] = PathUtility.GetRelativePathToFile(file.SiteUrl, GetXrefHref(file, uid, uidCount, obj.Parent == null));
                    }
                    return (errors, newObject);

                case JValue value:
                    return TransformScalar(schema, file, value);

                default:
                    throw new NotSupportedException();
            }
        }

        private (List<Error>, JToken) TransformScalar(JsonSchema schema, Document file, JValue value)
        {
            var errors = new List<Error>();
            if (value.Type == JTokenType.Null || schema.ContentType is null)
            {
                return (errors, value);
            }

            var sourceInfo = JsonUtility.GetSourceInfo(value);
            var content = new SourceInfo<string>(value.Value<string>(), sourceInfo);

            switch (schema.ContentType)
            {
                case JsonSchemaContentType.Href:
                    var (error, link, _) = _linkResolver.ResolveLink(content, file, file);
                    errors.AddIfNotNull(error);
                    return (errors, link);

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = _markdownEngine.ToHtml(content, file, MarkdownPipelineType.Markdown);
                    errors.AddRange(markupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return (errors, html);

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = _markdownEngine.ToHtml(content, file, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(inlineMarkupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return (errors, inlineHtml);

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:

                    var htmlWithLinks = HtmlUtility.TransformHtml(content, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                    {
                        HtmlUtility.TransformLink(ref token, null, href =>
                        {
                            var (htmlError, htmlLink, _) = _linkResolver.ResolveLink(new SourceInfo<string>(href, content), file, file);
                            errors.AddIfNotNull(htmlError);
                            return htmlLink;
                        });
                    });

                    return (errors, htmlWithLinks);

                case JsonSchemaContentType.Xref:
                    // the content here must be an UID, not href
                    var (xrefError, xrefSpec, href) = _xrefResolver.ResolveXrefSpec(content, file, file);
                    errors.AddIfNotNull(xrefError);

                    return xrefSpec != null
                        ? (errors, JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href)))
                        : (errors, new JObject { ["name"] = value, ["href"] = null });
            }

            return (errors, value);
        }
    }
}
