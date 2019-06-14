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

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token, Action<Document> buildChild)
        {
            var errors = new List<Error>();
            token = JsonUtility.DeepClone(token);
            Transform(file, context, _schema, token, errors, buildChild);
            return (errors, token);
        }

        public (List<Error> errors, Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)> properties) TransformXref(Document file, Context context, JToken token, Action<Document> buildChild)
        {
            var errors = new List<Error>();
            token = JsonUtility.DeepClone(token);
            var xrefPropertiesGroupByUid = new Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)>();
            var uidJsonPaths = new HashSet<string>();

            TransformXref(_schema, token, true);

            return (errors, xrefPropertiesGroupByUid);

            void TransformXref(JsonSchema schema, JToken node, bool isRoot = false)
            {
                schema = _definitions.GetDefinition(schema);

                switch (node)
                {
                    case JObject obj:

                        var uid = obj.TryGetValue("uid", out var uidValue) && uidValue is JValue uidJValue && uidJValue.Value is string uidStr ? uidStr : null;
                        if (uid != null)
                        {
                            if (uidJsonPaths.Add(uidValue.Path) && xrefPropertiesGroupByUid.ContainsKey(uid))
                            {
                                errors.Add(Errors.UidConflict(uid));
                                return;
                            }

                            if (!xrefPropertiesGroupByUid.TryGetValue(uid, out _))
                            {
                                xrefPropertiesGroupByUid[uid] = (isRoot, new Dictionary<string, Lazy<JToken>>());
                            }
                        }

                        foreach (var (key, value) in obj)
                        {
                            if (uid != null && schema.XrefProperties.Contains(key))
                            {
                                var propertySchema = TryGetPropertyJsonSchema(schema, key, out var subSchema) ? subSchema : null;
                                xrefPropertiesGroupByUid[uid].Item2[key] = new Lazy<JToken>(
                                () => Transform(file, context, propertySchema, value, errors, buildChild), LazyThreadSafetyMode.PublicationOnly);
                            }
                            else
                            {
                                if (TryGetPropertyJsonSchema(schema, key, out var propertySchema))
                                {
                                    TransformXref(propertySchema, value);
                                }
                            }
                        }
                        break;
                    case JArray array:

                        if (schema.Items != null)
                        {
                            foreach (var item in array)
                            {
                                TransformXref(schema.Items, item);
                            }
                        }
                        break;
                }
            }
        }

        private JToken Transform(Document file, Context context, JsonSchema schema, JToken token, List<Error> errors, Action<Document> buildChild)
        {
            schema = _definitions.GetDefinition(schema);
            if (schema == null)
            {
                return token;
            }

            switch (token)
            {
                case JValue scalar:
                    var transformedScalar = TransformScalar(schema, file, context, scalar, errors, buildChild);
                    scalar.Replace(transformedScalar);
                    break;

                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            Transform(file, context, schema.Items, item, errors, buildChild);
                        }
                    }
                    break;

                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (TryGetPropertyJsonSchema(schema, key, out var propertySchema))
                        {
                            Transform(file, context, propertySchema, value, errors, buildChild);
                        }
                    }
                    break;
            }

            return token;
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

        private JValue TransformScalar(JsonSchema schema, Document file, Context context, JValue value, List<Error> errors, Action<Document> buildChild)
        {
            if (value.Type == JTokenType.Null)
            {
                return value;
            }

            var dependencyResolver = file.Schema.Type == typeof(LandingData) ? context.LandingPageDependencyResolver : context.DependencyResolver;
            var sourceInfo = JsonUtility.GetSourceInfo(value);
            var content = new SourceInfo<string>(value.Value<string>(), sourceInfo);

            switch (schema.ContentType)
            {
                case JsonSchemaContentType.Href:
                    var (error, link, _) = dependencyResolver.ResolveLink(content, file, file, buildChild);
                    errors.AddIfNotNull(error);
                    content = new SourceInfo<string>(link, content);
                    break;

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = MarkdownUtility.ToHtml(
                        content,
                        file,
                        dependencyResolver,
                        buildChild,
                        context.MonikerProvider,
                        key => context.Template?.GetToken(key),
                        MarkdownPipelineType.Markdown);

                    errors.AddRange(markupErrors);
                    content = new SourceInfo<string>(html, content);
                    break;

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = MarkdownUtility.ToHtml(
                        content,
                        file,
                        dependencyResolver,
                        buildChild,
                        context.MonikerProvider,
                        key => context.Template?.GetToken(key),
                        MarkdownPipelineType.InlineMarkdown);

                    errors.AddRange(inlineMarkupErrors);
                    content = new SourceInfo<string>(inlineHtml, content);
                    break;

                case JsonSchemaContentType.Html:
                    var htmlWithLinks = HtmlUtility.TransformLinks(content, (href, _) =>
                    {
                        var (htmlError, htmlLink, _) = dependencyResolver.ResolveLink(new SourceInfo<string>(href, content), file, file, buildChild);
                        errors.AddIfNotNull(htmlError);
                        return htmlLink;
                    });

                    content = new SourceInfo<string>(htmlWithLinks, content);
                    break;

                case JsonSchemaContentType.Xref:

                    // TODO: how to fill xref resolving data besides href
                    var (xrefError, xrefLink, _, _) = dependencyResolver.ResolveXref(content, file, file);
                    errors.AddIfNotNull(xrefError);
                    content = new SourceInfo<string>(xrefLink, content);
                    break;
            }

            value = new JValue(content.Value);
            JsonUtility.SetSourceInfo(value, content);
            return value;
        }
    }
}
