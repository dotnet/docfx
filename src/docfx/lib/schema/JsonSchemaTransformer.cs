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
        private readonly ConcurrentDictionary<JToken, (List<Error>, JToken)> _xrefPropertiesCache =
                     new ConcurrentDictionary<JToken, (List<Error>, JToken)>(ReferenceEqualsComparer.Default);

        private static ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                 = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            return TransformContentCore(file, context, _schema, token);
        }

        public (List<Error>, IReadOnlyList<InternalXrefSpec>) LoadXrefSpecs(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefSpecs = new List<InternalXrefSpec>();
            LoadXrefSpecsCore(file, context, _schema, token, errors, xrefSpecs);

            // if only one uid defined in the file, remove the bookmark from href if any
            if (xrefSpecs.Count == 1)
            {
                xrefSpecs[0].Href = xrefSpecs[0].Href.Split('#')[0];
            }
            return (errors, xrefSpecs);
        }

        private void LoadXrefSpecsCore(Document file, Context context, JsonSchema schema, JToken node, List<Error> errors, List<InternalXrefSpec> xrefSpecs)
        {
            schema = _definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
                    if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string uid &&
                        schema.Properties.TryGetValue("uid", out var uidSchema) && uidSchema.ContentType == JsonSchemaContentType.Uid)
                    {
                        xrefSpecs.Add(LoadXrefSpec(file, context, schema, new SourceInfo<string>(uid, uidValue.GetSourceInfo()), obj));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            LoadXrefSpecsCore(file, context, propertySchema, value, errors, xrefSpecs);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(file, context, schema.Items.schema, item, errors, xrefSpecs);
                    }
                    break;
            }
        }

        private InternalXrefSpec LoadXrefSpec(Document file, Context context, JsonSchema schema, SourceInfo<string> uid, JObject obj)
        {
            var fragment = $"#{Regex.Replace(uid, @"\W", "_")}";
            var href = obj.Parent is null ? file.SiteUrl : UrlUtility.MergeUrl(file.SiteUrl, "", fragment);
            var xref = new InternalXrefSpec(uid, href, file);

            foreach (var xrefProperty in schema.XrefProperties)
            {
                if (!obj.TryGetValue(xrefProperty, out var value))
                {
                    continue;
                }

                if (!schema.Properties.TryGetValue(xrefProperty, out var propertySchema))
                {
                    xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => value);
                    continue;
                }

                xref.XrefProperties[xrefProperty] = new Lazy<JToken>(
                    () => LoadXrefProperty(file, context, uid, value, propertySchema),
                    LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private JToken LoadXrefProperty(Document file, Context context, SourceInfo<string> uid, JToken value, JsonSchema schema)
        {
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
            }

            try
            {
                recursionDetector.Push(uid);
                var (transformErrors, transformedToken) = _xrefPropertiesCache.GetOrAdd(value, _ => TransformContentCore(file, context, schema, value));
                context.ErrorLog.Write(transformErrors);
                return transformedToken;
            }
            finally
            {
                Debug.Assert(recursionDetector.Count > 0);
                recursionDetector.Pop();
            }
        }

        private (List<Error>, JToken) TransformContentCore(Document file, Context context, JsonSchema schema, JToken token)
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
                        var (arrayErrors, newItem) = TransformContentCore(file, context, schema.Items.schema, item);
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
                            var (propertyErrors, transformedValue) = isXrefProperty
                                ? _xrefPropertiesCache.GetOrAdd(value, _ => TransformContentCore(file, context, propertySchema, value))
                                : TransformContentCore(file, context, propertySchema, value);
                            errors.AddRange(propertyErrors);
                            newObject[key] = transformedValue;
                        }
                        else
                        {
                            newObject[key] = value;
                        }
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
                    return (errors, HtmlUtility.LoadHtml(html).PostMarkup(context.Config.DryRun).WriteTo());

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(inlineMarkupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return (errors, HtmlUtility.LoadHtml(inlineHtml).PostMarkup(context.Config.DryRun).WriteTo());

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:
                    var htmlWithLinks = HtmlUtility.TransformLinks(content, (href, _) =>
                    {
                        var (htmlError, htmlLink, _) = context.LinkResolver.ResolveLink(
                            new SourceInfo<string>(href, content), file, file);
                        errors.AddIfNotNull(htmlError);
                        return htmlLink;
                    });

                    return (errors, htmlWithLinks);

                case JsonSchemaContentType.Xref:
                    // the content here must be an UID, not href
                    var (xrefError, xrefSpec, href) = context.XrefResolver.ResolveXrefSpec(content, file, file);
                    errors.AddIfNotNull(xrefError);

                    if (xrefSpec != null)
                    {
                        var specObj = JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href));
                        return (errors, specObj);
                    }

                    return (errors, value);
            }

            return (errors, value);
        }
    }
}
