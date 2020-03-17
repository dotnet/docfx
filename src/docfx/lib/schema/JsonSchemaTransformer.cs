// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private static ThreadLocal<Stack<(string uid, Document declaringFile)>> t_recursionDetector
                 = new ThreadLocal<Stack<(string, Document)>>(() => new Stack<(string, Document)>());

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
            return (errors, xrefSpecs);
        }

        private void LoadXrefSpecsCore(Document file, Context context, JsonSchema schema, JToken node, List<Error> errors, List<InternalXrefSpec> xrefSpecs)
        {
            schema = _definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string uid)
                    {
                        xrefSpecs.Add(LoadXrefSpec(file, context, schema, uid, obj));
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

        private InternalXrefSpec LoadXrefSpec(Document file, Context context, JsonSchema schema, string uid, JObject obj)
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
                    xref.ExtensionData[xrefProperty] = new Lazy<JToken>(() => value);
                    continue;
                }

                xref.ExtensionData[xrefProperty] = new Lazy<JToken>(() =>
                {
                    var recursionDetector = t_recursionDetector.Value!;
                    if (recursionDetector.Contains((uid, file)))
                    {
                        var referenceMap = recursionDetector.Select(x => $"{x.uid} ({x.declaringFile})").Reverse().ToList();
                        referenceMap.Add($"{uid} ({file})");
                        throw Errors.Link.CircularReference(referenceMap, file).ToException();
                    }

                    try
                    {
                        recursionDetector.Push((uid, file));
                        var (transformErrors, transformedToken) = _xrefPropertiesCache.GetOrAdd(value, _ => TransformContentCore(file, context, propertySchema, value));
                        context.ErrorLog.Write(transformErrors);
                        return transformedToken;
                    }
                    finally
                    {
                        Debug.Assert(recursionDetector.Count > 0);
                        recursionDetector.Pop();
                    }
                }, LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private (List<Error>, JToken) TransformContentCore(Document file, Context context, JsonSchema schema, JToken token)
        {
            if (_xrefPropertiesCache.TryGetValue(token, out var result))
            {
                return result;
            }

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
                            var (propertyErrors, transformedValue) = TransformContentCore(file, context, propertySchema, value);
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
            if (value.Type == JTokenType.Null || schema.ContentType == JsonSchemaContentType.None)
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
                    var (xrefError, xrefSpec) = context.XrefResolver.ResolveXrefSpec(content, file);
                    errors.AddIfNotNull(xrefError);

                    if (xrefSpec != null)
                    {
                        var specObj = JsonUtility.ToJObject(xrefSpec);
                        JsonUtility.SetSourceInfo(specObj, content);
                        return (errors, specObj);
                    }

                    return (errors, JValue.CreateNull());
            }

            return (errors, value);
        }
    }
}
