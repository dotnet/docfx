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
        private readonly ConcurrentDictionary<JToken, (List<Error>, JToken)> _transformedProperties;
        private static ThreadLocal<Stack<(string uid, Document declaringFile)>> t_recursionDetector
            = new ThreadLocal<Stack<(string, Document)>>(() => new Stack<(string, Document)>());

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
            _transformedProperties = new ConcurrentDictionary<JToken, (List<Error>, JToken)>(ReferenceEqualsComparer.Default);
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            return TransformToken(file, context, _schema, token);
        }

        public (List<Error>, IReadOnlyList<InternalXrefSpec>) LoadXrefSpecs(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var uids = new HashSet<string>();

            return (errors, LoadXrefSpecs(_schema, token));

            List<InternalXrefSpec> LoadXrefSpecs(JsonSchema schema, JToken node)
            {
                var xrefSpecs = new List<InternalXrefSpec>();
                schema = _definitions.GetDefinition(schema);
                switch (node)
                {
                    case JObject obj:
                        if (!schema.Properties.TryGetValue("uid", out var uidSchema) || uidSchema.ContentType != JsonSchemaContentType.Uid)
                        {
                            TraverseObjectXref(obj);
                            break;
                        }

                        if (!obj.TryGetValue<JValue>("uid", out var uidValue) || !(uidValue.Value is string uid))
                        {
                            TraverseObjectXref(obj);
                            break;
                        }

                        if (uids.Add(uid))
                        {
                            xrefSpecs.Add(GetXrefSpec(uid, obj));
                        }
                        else
                        {
                            errors.Add(Errors.UidConflict(uid, JsonUtility.GetSourceInfo(uidValue)));
                        }

                        break;
                    case JArray array:
                        foreach (var item in array)
                        {
                            if (schema.Items.schema != null)
                                xrefSpecs.AddRange(LoadXrefSpecs(schema.Items.schema, item));
                        }
                        break;
                }

                return xrefSpecs;

                InternalXrefSpec GetXrefSpec(string uid, JObject obj)
                {
                    var contentTypeProperties = new Dictionary<string, JsonSchemaContentType>();
                    var xrefProperties = new Dictionary<string, Lazy<JToken>>();
                    TraverseObjectXref(obj, (propertySchema, key, value) =>
                    {
                        if (schema.XrefProperties.Contains(key))
                        {
                            contentTypeProperties[key] = propertySchema.ContentType;
                            xrefProperties[key] = new Lazy<JToken>(
                                () =>
                                {
                                    if (t_recursionDetector.Value.Contains((uid, file)))
                                    {
                                        var referenceMap = t_recursionDetector.Value.Select(x => $"{x.uid} ({x.declaringFile})").Reverse().ToList();
                                        referenceMap.Add($"{uid} ({file})");
                                        throw Errors.CircularReference(referenceMap, file).ToException();
                                    }

                                    try
                                    {
                                        t_recursionDetector.Value.Push((uid, file));
                                        var (transformErrors, transformedToken) = TransformToken(file, context, propertySchema, value);
                                        context.ErrorLog.Write(transformErrors);
                                        return transformedToken;
                                    }
                                    finally
                                    {
                                        Debug.Assert(t_recursionDetector.Value.Count > 0);
                                        t_recursionDetector.Value.Pop();
                                    }
                                }, LazyThreadSafetyMode.PublicationOnly);
                            return true;
                        }

                        return false;
                    });

                    var xref = new InternalXrefSpec
                    {
                        Uid = uid,
                        Source = JsonUtility.GetSourceInfo(obj),
                        Href = obj.Parent is null ? file.SiteUrl : UrlUtility.MergeUrl(file.SiteUrl, "", $"#{GetBookmarkFromUid(uid)}"),
                        DeclaringFile = file,
                    };
                    xref.ExtensionData.AddRange(xrefProperties);
                    xref.PropertyContentTypeMapping.AddRange(contentTypeProperties);
                    return xref;
                }

                string GetBookmarkFromUid(string uid)
                    => Regex.Replace(uid, @"\W", "_");

                void TraverseObjectXref(JObject obj, Func<JsonSchema, string, JToken, bool> action = null)
                {
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            if (action?.Invoke(propertySchema, key, value) ?? false)
                                continue;

                            xrefSpecs.AddRange(LoadXrefSpecs(propertySchema, value));
                        }
                    }
                }
            }
        }

        private (List<Error>, JToken) TransformToken(Document file, Context context, JsonSchema schema, JToken token)
        {
            schema = _definitions.GetDefinition(schema);

            if (schema == null)
            {
                return (new List<Error>(), token);
            }

            return _transformedProperties.GetOrAdd(token, _ =>
            {
                var errors = new List<Error>();
                switch (token)
                {
                    // transform array and object is not supported yet
                    case JArray array:
                        var newArray = new JArray();
                        foreach (var item in array)
                        {
                            var (arrayErrors, newItem) = TransformToken(file, context, schema.Items.schema, item);
                            errors.AddRange(arrayErrors);
                            newArray.Add(newItem);
                        }

                        return (errors, newArray);

                    case JObject obj:
                        var newObject = new JObject();
                        foreach (var (key, value) in obj)
                        {
                            if (schema.Properties.TryGetValue(key, out var propertySchema))
                            {
                                var (propertyErrors, transformedValue) = TransformToken(file, context, propertySchema, value);
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
            });
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
                    content = new SourceInfo<string>(link, content);
                    break;

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.Markdown);
                    errors.AddRange(markupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    content = new SourceInfo<string>(HtmlUtility.LoadHtml(html).PostMarkup(context.Config.DryRun).WriteTo(), content);
                    break;

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(inlineMarkupErrors);

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    content = new SourceInfo<string>(HtmlUtility.LoadHtml(inlineHtml).PostMarkup(context.Config.DryRun).WriteTo(), content);
                    break;

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:
                    var htmlWithLinks = HtmlUtility.TransformLinks(content, (href, _) =>
                    {
                        var (htmlError, htmlLink, _) = context.LinkResolver.ResolveLink(
                            new SourceInfo<string>(href, content), file, file);
                        errors.AddIfNotNull(htmlError);
                        return htmlLink;
                    });

                    content = new SourceInfo<string>(htmlWithLinks, content);
                    break;

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

                    content = new SourceInfo<string>(null, content);
                    break;
            }

            value = new JValue(content.Value);
            JsonUtility.SetSourceInfo(value, content);
            return (errors, value);
        }
    }
}
