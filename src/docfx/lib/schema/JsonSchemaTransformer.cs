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
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;

        private readonly ConcurrentDictionary<Document, int> _uidCountCache =
                     new ConcurrentDictionary<Document, int>(ReferenceEqualsComparer.Default);

        private static ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                 = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(_schema, token));
            return TransformContentCore(file, context, _schema, token, uidCount);
        }

        public (List<Error>, IReadOnlyList<InternalXrefSpec>) LoadXrefSpecs(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefSpecs = new List<InternalXrefSpec>();
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(_schema, token));
            LoadXrefSpecsCore(file, context, _schema, token, errors, xrefSpecs, uidCount);
            return (errors, xrefSpecs);
        }

        private void LoadXrefSpecsCore(Document file, Context context, JsonSchema schema, JToken node, List<Error> errors, List<InternalXrefSpec> xrefSpecs, int uidCount)
        {
            schema = _definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out var uid))
                    {
                        xrefSpecs.Add(LoadXrefSpec(file, context, schema, uid, obj, uidCount));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            LoadXrefSpecsCore(file, context, propertySchema, value, errors, xrefSpecs, uidCount);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(file, context, schema.Items.schema, item, errors, xrefSpecs, uidCount);
                    }
                    break;
            }
        }

        private InternalXrefSpec LoadXrefSpec(Document file, Context context, JsonSchema schema, SourceInfo<string> uid, JObject obj, int uidCount)
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
                    () => LoadXrefProperty(file, context, uid, value, propertySchema, uidCount),
                    LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private int GetFileUidCount(JsonSchema schema, JToken node)
        {
            schema = _definitions.GetDefinition(schema);
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
                            count += GetFileUidCount(propertySchema, value);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        count += GetFileUidCount(schema.Items.schema, item);
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

        private JToken LoadXrefProperty(Document file, Context context, SourceInfo<string> uid, JToken value, JsonSchema schema, int uidCount)
        {
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
            }

            try
            {
                recursionDetector.Push(uid);
                var (transformErrors, transformedToken) = TransformContentCore(file, context, schema, value, uidCount);
                context.ErrorLog.Write(transformErrors);
                return transformedToken;
            }
            finally
            {
                Debug.Assert(recursionDetector.Count > 0);
                recursionDetector.Pop();
            }
        }

        private (List<Error>, JToken) TransformContentCore(Document file, Context context, JsonSchema schema, JToken token, int uidCount)
        {
            var errors = new List<Error>();
            schema = _definitions.GetDefinition(schema);
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
                        var (arrayErrors, newItem) = TransformContentCore(file, context, schema.Items.schema, item, uidCount);
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
                            var isXrefProperty = schema.XrefProperties.Contains(key);
                            var (propertyErrors, transformedValue) = TransformContentCore(file, context, propertySchema, value, uidCount);
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
                    return TransformScalar(schema, file, context, value);

                default:
                    throw new NotSupportedException();
            }
        }

        private (List<Error>, JToken) TransformScalar(JsonSchema schema, Document file, Context context, JValue value)
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
                    var (error, link, _) = context.LinkResolver.ResolveLink(content, file, file);
                    errors.AddIfNotNull(error);
                    return (errors, link);

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.Markdown);
                    errors.AddRange(markupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return (errors, html);

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(inlineMarkupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return (errors, inlineHtml);

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:

                    var htmlWithLinks = HtmlUtility.TransformHtml(content, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                    {
                        HtmlUtility.TransformLink(ref token, null, href =>
                        {
                            var (htmlError, htmlLink, _) = context.LinkResolver.ResolveLink(new SourceInfo<string>(href, content), file, file);
                            errors.AddIfNotNull(htmlError);
                            return htmlLink;
                        });
                    });

                    return (errors, htmlWithLinks);

                case JsonSchemaContentType.Xref:
                    // the content here must be an UID, not href
                    var (xrefError, xrefSpec, href) = context.XrefResolver.ResolveXrefSpec(content, file, file);
                    errors.AddIfNotNull(xrefError);

                    return xrefSpec != null
                        ? (errors, JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href)))
                        : (errors, new JObject { ["name"] = value, ["href"] = null });
            }

            return (errors, value);
        }
    }
}
