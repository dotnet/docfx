// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            Traverse(_schema, token, (schema, value) => value.Replace(TransformScalar(schema, file, context, value, errors, buildChild)));
            return (errors, token);
        }

        public (List<Error> errors, Dictionary<string, Lazy<JValue>>) TransformXref(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var extensions = new Dictionary<string, Lazy<JValue>>();
            Traverse(_schema, token, TransformXrefScalar);
            return (errors, extensions);

            void TransformXrefScalar(JsonSchema schema, JValue value)
            {
                extensions[value.Path] = new Lazy<JValue>(
                    () => TransformValue(schema, file, context, value, errors, buildChild: null),
                    LazyThreadSafetyMode.PublicationOnly);
            }
        }

        private void Traverse(JsonSchema schema, JToken token, Action<JsonSchema, JValue> transformScalar)
        {
            schema = _definitions.GetDefinition(schema);

            switch (token)
            {
                case JValue scalar:
                    transformScalar(schema, scalar);
                    break;

                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            Traverse(schema.Items, item, transformScalar);
                        }
                    }
                    break;

                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            Traverse(propertySchema, value, transformScalar);
                        }
                    }
                    break;
            }
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
                        null,
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
                        null,
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
