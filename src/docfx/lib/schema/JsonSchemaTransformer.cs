// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var transformedToken = Transform(file, context, _schema, token, errors);
            return (errors, transformedToken);
        }

        public (List<Error> errors, Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)> properties) TransformXref(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefPropertiesGroupByUid = new Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)>();
            var uidJsonPaths = new HashSet<string>();

            Traverse(_schema, token, (schema, node) =>
            {
                if (node is JObject obj)
                {
                    var uid = obj.TryGetValue("uid", out var uidValue) && uidValue is JValue uidJValue && uidJValue.Value is string uidStr ? uidStr : null;

                    if (uid == null)
                    {
                        return (default, node);
                    }

                    if (uidJsonPaths.Add(uidValue.Path) && xrefPropertiesGroupByUid.ContainsKey(uid))
                    {
                        errors.Add(Errors.UidConflict(uid));
                        return (default, node);
                    }

                    if (!xrefPropertiesGroupByUid.TryGetValue(uid, out _))
                    {
                        xrefPropertiesGroupByUid[uid] = (obj.Parent == null, new Dictionary<string, Lazy<JToken>>());
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (schema.XrefProperties.Contains(key))
                        {
                            var propertySchema = TryGetPropertyJsonSchema(schema, key, out var subSchema) ? subSchema : null;
                            xrefPropertiesGroupByUid[uid].Item2[key] = new Lazy<JToken>(
                            () =>
                            {
                                return Transform(file, context, propertySchema, value, errors);
                            }, LazyThreadSafetyMode.PublicationOnly);
                        }
                    }
                    return (schema.XrefProperties, node);
                }

                return (default, node);
            });

            return (errors, xrefPropertiesGroupByUid);
        }

        private JToken Transform(Document file, Context context, JsonSchema schema, JToken token, List<Error> errors)
        {
            return Traverse(schema, token, (subSchema, node) =>
            {
                if (node.Type == JTokenType.Array || node.Type == JTokenType.Object)
                {
                    // don't support array/object transform now
                    return (default, node);
                }

                var transformedScalar = TransformScalar(subSchema, file, context, node as JValue, errors);
                return (default, transformedScalar);
            });
        }

        private JToken Traverse(JsonSchema schema, JToken token, Func<JsonSchema, JToken, (string[], JToken)> transform)
        {
            schema = _definitions.GetDefinition(schema);
            if (schema == null)
            {
                return token;
            }

            var (transformedKeys, transformedToken) = transform(schema, token);
            transformedKeys = transformedKeys ?? Array.Empty<string>();

            switch (transformedToken)
            {
                case JValue scalar:

                    return scalar;

                case JArray array:

                    if (schema.Items == null || schema.ContentType != JsonSchemaContentType.None)
                        return array;

                    var newArray = new JArray();
                    foreach (var item in array)
                    {
                        var newItem = Traverse(schema.Items, item, transform);
                        newArray.Add(newItem);
                    }

                    return newArray;

                case JObject obj:

                    var newObject = new JObject();
                    foreach (var (key, value) in obj)
                    {
                        if (!transformedKeys.Contains(key) && TryGetPropertyJsonSchema(schema, key, out var propertySchema))
                        {
                            newObject[key] = Traverse(propertySchema, value, transform);
                        }
                        else
                        {
                            newObject[key] = value;
                        }
                    }
                    return newObject;
            }

            throw new NotSupportedException();
        }

        private static bool TryGetPropertyJsonSchema(JsonSchema jsonSchema, string key, out JsonSchema propertySchema)
        {
            propertySchema = null;
            if (jsonSchema == null)
            {
                return false;
            }

            if (jsonSchema.Properties.TryGetValue(key, out propertySchema))
            {
                return true;
            }

            if (jsonSchema.AdditionalProperties.additionalPropertyJsonSchema != null)
            {
                propertySchema = jsonSchema.AdditionalProperties.additionalPropertyJsonSchema;
                return true;
            }

            return false;
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

                    var (xrefError, xrefLink, _, xrefSpec) = context.DependencyResolver.ResolveXref(content, file);

                    if (xrefSpec is InternalXrefSpec internalSpec)
                    {
                        xrefSpec = internalSpec.ToExternalXrefSpec(context, file);
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
