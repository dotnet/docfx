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
        private readonly ConcurrentDictionary<Document, Dictionary<JToken, Lazy<JToken>>> _properties;

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var transformedToken = TransformToken(file, context, _schema, token, errors);
            return (errors, transformedToken);
        }

        public (List<Error> errors, Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)> properties) TransformXref(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefPropertiesGroupByUid = new Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)>();
            var uidJsonPaths = new HashSet<string>();

            TransformXref(_schema, token);

            return (errors, xrefPropertiesGroupByUid);

            JToken TransformXref(JsonSchema schema, JToken node)
            {
                schema = _definitions.GetDefinition(schema);
                switch (node)
                {
                    case JObject obj:
                        var uid = obj.TryGetValue("uid", out var uidValue) && uidValue is JValue uidJValue && uidJValue.Value is string uidStr ? uidStr : null;

                        if (uid == null)
                        {
                            return TransformObject(schema, obj, (a, b, c) => TransformXref(a, c));
                        }

                        if (uidJsonPaths.Add(uidValue.Path) && xrefPropertiesGroupByUid.ContainsKey(uid))
                        {
                            // TODO: should throw warning and take the first one order by json path
                            errors.Add(Errors.UidConflict(uid));
                            return TransformObject(schema, obj, (a, b, c) => TransformXref(a, c));
                        }

                        if (!xrefPropertiesGroupByUid.TryGetValue(uid, out _))
                        {
                            xrefPropertiesGroupByUid[uid] = (obj.Parent == null, new Dictionary<string, Lazy<JToken>>());
                        }

                        return TransformObject(schema, obj, (propertySchema, propertyKey, propertyValue) =>
                        {
                            if (!schema.XrefProperties.Contains(propertyKey))
                            {
                                return TransformXref(propertySchema, propertyValue);
                            }

                            xrefPropertiesGroupByUid[uid].Item2[propertyKey] = new Lazy<JToken>(
                                () =>
                                {
                                    return TransformToken(file, context, propertySchema, propertyValue, errors);
                                }, LazyThreadSafetyMode.PublicationOnly);

                            return propertyValue;
                        });

                    case JArray array:
                        return TransformArray(schema, array, TransformXref);
                }

                return node;
            }
        }

        private JToken TransformToken(Document file, Context context, JsonSchema schema, JToken token, List<Error> errors)
        {
            return TransformToken(schema, token);

            JToken TransformToken(JsonSchema subSchema, JToken node)
            {
                subSchema = _definitions.GetDefinition(subSchema);
                switch (node)
                {
                    // transform array and object is not supported yet
                    case JArray array:
                        return TransformArray(subSchema, array, TransformToken);

                    case JObject obj:
                        return TransformObject(subSchema, obj, (a, b, c) => TransformToken(a, c));

                    case JValue value:
                        return TransformScalar(subSchema, file, context, value, errors);
                }

                throw new NotSupportedException();
            }
        }

        private JArray TransformArray(JsonSchema schema, JArray array, Func<JsonSchema, JToken, JToken> transform)
        {
            var newArray = new JArray();
            foreach (var item in array)
            {
                var newItem = transform(schema.Items, item);
                newArray.Add(newItem);
            }

            return newArray;
        }

        private JObject TransformObject(JsonSchema schema, JObject obj, Func<JsonSchema, string, JToken, JToken> transform)
        {
            var newObject = new JObject();
            foreach (var (key, value) in obj)
            {
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    newObject[key] = transform(propertySchema, key, value);
                }
                else
                {
                    newObject[key] = value;
                }
            }
            return newObject;
        }

        private JToken TransformScalar(JsonSchema schema, Document file, Context context, JValue value, List<Error> errors)
        {
            if (value.Type == JTokenType.Null || schema.ContentType == JsonSchemaContentType.None)
            {
                return value;
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
                        return specObj;
                    }

                    content = new SourceInfo<string>(null, content);
                    break;
            }

            value = new JValue(content.Value);
            JsonUtility.SetSourceInfo(value, content);
            return value;
        }
    }
}
