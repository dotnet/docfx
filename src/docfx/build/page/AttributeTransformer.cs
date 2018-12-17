// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class AttributeTransformer
    {
        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> Transform(
            List<Error> errors,
            Document file,
            DependencyResolver dependencyResolver,
            Action<Document> buildChild,
            JObject extensionData = null)
        {
            return TransformXrefSpec;

            object TransformXrefSpec(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                var result = TransformContent(attribute, value);

                if (extensionData != null && attributes.Any(attr => attr is XrefPropertyAttribute))
                {
                    extensionData[jsonPath] = new JValue(result);
                }

                return result;
            }

            object TransformContent(DataTypeAttribute attribute, object value)
            {
                if (attribute is HrefAttribute)
                {
                    var (error, link, _) = dependencyResolver.ResolveLink((string)value, file, file, buildChild);
                    errors.AddIfNotNull(error);
                    return link;
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, dependencyResolver, buildChild, null, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, dependencyResolver, buildChild, null, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is HtmlAttribute)
                {
                    var html = HtmlUtility.TransformLinks((string)value, href =>
                    {
                        var (error, link, _) = dependencyResolver.ResolveLink(href, file, file, buildChild);
                        errors.AddIfNotNull(error);
                        return link;
                    });
                    return HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
                }

                if (attribute is XrefAttribute)
                {
                    // TODO: how to fill xref resolving data besides href
                    var (error, link, _) = dependencyResolver.ResolveXref((string)value, file);
                    errors.AddIfNotNull(error);
                    return link;
                }

                return value;
            }
        }
    }
}
