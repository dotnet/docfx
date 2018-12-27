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
            Dictionary<string, Lazy<JValue>> extensionData = null)
        {
            return TransformXrefSpec;

            object TransformXrefSpec(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                if (extensionData != null && attributes.Any(attr => attr is XrefPropertyAttribute))
                {
                    var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                    extensionData[jsonPath] = new Lazy<JValue>(() => new JValue(TransformContent(attribute, value)));
                    return null;
                }
                else
                {
                    var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                    return TransformContent(attribute, value);
                }
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
                    var (html, markup) = MarkdownUtility.ToHtml((string)value, file, dependencyResolver, buildChild, null, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = MarkdownUtility.ToHtml((string)value, file, dependencyResolver, buildChild, null, MarkdownPipelineType.InlineMarkdown);
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
                    var (error, link, _, _) = dependencyResolver.ResolveXref((string)value, file);
                    errors.AddIfNotNull(error);
                    return link;
                }

                return value;
            }
        }
    }
}
