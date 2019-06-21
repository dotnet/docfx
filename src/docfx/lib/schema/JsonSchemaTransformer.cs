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
            Transform(file, context, _schema, token, errors);
            return (errors, token);
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
                        return default;
                    }

                    if (uidJsonPaths.Add(uidValue.Path) && xrefPropertiesGroupByUid.ContainsKey(uid))
                    {
                        errors.Add(Errors.UidConflict(uid));
                        return default;
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
                                // todo: change transform to `return` model instead of `replace` model
                                var clonedObj = JsonUtility.DeepClone(obj);
                                Transform(file, context, propertySchema, clonedObj[key], errors);
                                return clonedObj[key];
                            }, LazyThreadSafetyMode.PublicationOnly);
                        }
                    }
                    return schema.XrefProperties;
                }

                return default;
            });

            return (errors, xrefPropertiesGroupByUid);
        }

        private void Transform(Document file, Context context, JsonSchema schema, JToken token, List<Error> errors)
        {
            Traverse(schema, token, (subSchema, node) =>
            {
                if (node.Type == JTokenType.Array || node.Type == JTokenType.Object)
                {
                    // don't support array/object transform now
                    return default;
                }

                node.Replace(TransformScalar(subSchema, file, context, node as JValue, errors));

                return default;
            });
        }

        private void Traverse(JsonSchema schema, JToken token, Func<JsonSchema, JToken, string[]> transform)
        {
            schema = _definitions.GetDefinition(schema);
            if (schema == null)
            {
                return;
            }

            var transformedKeys = transform(schema, token) ?? Array.Empty<string>();

            switch (token)
            {
                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            Traverse(schema.Items, item, transform);
                        }
                    }
                    break;

                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (!transformedKeys.Contains(key) && TryGetPropertyJsonSchema(schema, key, out var propertySchema))
                        {
                            Traverse(propertySchema, value, transform);
                        }
                    }
                    break;
            }
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
            if (value.Type == JTokenType.Null)
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

                    var (xrefError, xrefSpec) = context.XrefMapProvider.ResolveXrefSpec(content);
                    errors.AddIfNotNull(xrefError);

                    if (xrefSpec is InternalXrefSpec internalSpec)
                    {
                        xrefSpec = internalSpec.ToExternalXrefSpec();
                    }

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
