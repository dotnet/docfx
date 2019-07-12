// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaTransformer
    {
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;
        private readonly ConcurrentDictionary<JToken, (List<Error>, JToken)> _transformedProperties;

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

        public (List<Error> errors, Dictionary<string, List<(bool isRoot, SourceInfo source, Dictionary<string, Lazy<JToken>> propertiesByUid)>> xrefPropertiesGroupByUid) TraverseXref(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefPropertiesGroupByUid = new Dictionary<string, List<(bool, SourceInfo, Dictionary<string, Lazy<JToken>>)>>();

            TraverseXref(_schema, token);

            return (errors, xrefPropertiesGroupByUid);

            void TraverseXref(JsonSchema schema, JToken node)
            {
                schema = _definitions.GetDefinition(schema);
                switch (node)
                {
                    case JObject obj:
                        var uid = obj.TryGetValue("uid", out var uidValue) && uidValue is JValue uidJValue && uidJValue.Value is string uidStr ? uidStr : null;

                        if (uid is null)
                        {
                            TraverseObjectXref(obj);
                            break;
                        }

                        if (!xrefPropertiesGroupByUid.TryGetValue(uid, out var xrefPropertyList))
                        {
                            xrefPropertiesGroupByUid[uid] = xrefPropertyList = new List<(bool, SourceInfo, Dictionary<string, Lazy<JToken>>)>();
                        }

                        var xrefProperties = new Dictionary<string, Lazy<JToken>>();
                        TraverseObjectXref(obj, (propertySchema, key, value) =>
                        {
                            if (schema.XrefProperties.Contains(key))
                            {
                                xrefProperties[key] = new Lazy<JToken>(
                                    () =>
                                    {
                                        var (transformErrors, transformedToken) = TransformToken(file, context, propertySchema, value);
                                        context.ErrorLog.Write(transformErrors);
                                        return transformedToken;
                                    }, LazyThreadSafetyMode.PublicationOnly);
                                return true;
                            }

                            return false;
                        });
                        xrefPropertyList.Add((obj.Parent is null, JsonUtility.GetSourceInfo(obj), xrefProperties));

                        break;
                    case JArray array:
                        foreach (var item in array)
                        {
                            if (schema.Items != null)
                                TraverseXref(schema.Items, item);
                        }
                        break;
                }

                void TraverseObjectXref(JObject obj, Func<JsonSchema, string, JToken, bool> action = null)
                {
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            if (action?.Invoke(propertySchema, key, value) ?? false)
                                continue;

                            TraverseXref(propertySchema, value);
                        }
                    }
                }
            }
        }

        private (List<Error>, JToken) TransformToken(Document file, Context context, JsonSchema schema, JToken token)
        {
            return TransformToken(schema, token);

            (List<Error>, JToken) TransformToken(JsonSchema subSchema, JToken node)
            {
                subSchema = _definitions.GetDefinition(subSchema);

                if (subSchema == null)
                {
                    return (new List<Error>(), node);
                }

                return _transformedProperties.GetOrAdd(node, _ =>
                {
                    var errors = new List<Error>();
                    switch (node)
                    {
                        // transform array and object is not supported yet
                        case JArray array:
                            var newArray = new JArray();
                            foreach (var item in array)
                            {
                                var (arrayErrors, newItem) = TransformToken(subSchema.Items, item);
                                errors.AddRange(arrayErrors);
                                newArray.Add(newItem);
                            }

                            return (errors, newArray);

                        case JObject obj:
                            var newObject = new JObject();
                            foreach (var (key, value) in obj)
                            {
                                if (subSchema.Properties.TryGetValue(key, out var propertySchema))
                                {
                                    var (propertyErrors, transformedValue) = TransformToken(propertySchema, value);
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
                            return TransformScalar(subSchema, file, context, value);

                        default:
                            throw new NotSupportedException();
                    }
                });
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
                    var (error, link, _) = context.DependencyResolver.ResolveRelativeLink(file, content, file);
                    errors.AddIfNotNull(error);
                    content = new SourceInfo<string>(link, content);
                    break;

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = MarkdownUtility.ToHtml(
                        context,
                        content,
                        file,
                        MarkdownPipelineType.Markdown);

                    errors.AddRange(markupErrors);
                    content = new SourceInfo<string>(html, content);
                    break;

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = MarkdownUtility.ToHtml(
                        context,
                        content,
                        file,
                        MarkdownPipelineType.InlineMarkdown);

                    errors.AddRange(inlineMarkupErrors);
                    content = new SourceInfo<string>(inlineHtml, content);
                    break;

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:
                    var htmlWithLinks = HtmlUtility.TransformLinks(content, (href, _) =>
                    {
                        var (htmlError, htmlLink, _) = context.DependencyResolver.ResolveRelativeLink(
                            file, new SourceInfo<string>(href, content), file);
                        errors.AddIfNotNull(htmlError);
                        return htmlLink;
                    });

                    content = new SourceInfo<string>(htmlWithLinks, content);
                    break;

                case JsonSchemaContentType.Xref:

                    var (xrefError, xrefLink, _, xrefSpec) = context.DependencyResolver.ResolveAbsoluteXref(content, file);

                    if (xrefSpec is InternalXrefSpec internalSpec)
                    {
                        xrefSpec = internalSpec.ToExternalXrefSpec(context, forXrefMapOutput: false);
                    }
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
