// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaTransform
    {
        public static (List<Error> errors, JToken token) Transform(Document file, Context context, JsonSchema schema, JToken token, Action<Document> buildChild)
        {
            var errors = new List<Error>();

            Transform(new JsonSchemaContext(schema), file, context, schema, token, errors, buildChild);
            return (errors, token);
        }

        private static void Transform(JsonSchemaContext jsonSchemaContext, Document file, Context context, JsonSchema schema, JToken token, List<Error> errors, Action<Document> buildChild)
        {
            schema = jsonSchemaContext.GetDefinition(schema);

            switch (token)
            {
                case JValue scalar:
                    token.Replace(TransformScalar(file, context, schema, scalar, errors, buildChild));
                    break;
                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            Transform(jsonSchemaContext, file, context, schema.Items, item, errors, buildChild);
                        }
                    }
                    break;
                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            Transform(jsonSchemaContext, file, context, propertySchema, value, errors, buildChild);
                        }
                    }
                    break;
            }
        }

        private static JValue TransformScalar(Document file, Context context, JsonSchema schema, JValue value, List<Error> errors, Action<Document> buildChild)
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

                    content = new SourceInfo<string>(HtmlUtility.StripTags(HtmlUtility.LoadHtml(htmlWithLinks)).OuterHtml, content);
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
