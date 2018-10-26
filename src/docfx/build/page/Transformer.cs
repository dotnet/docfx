// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Transformer
    {
        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> Transform(List<Error> errors, MarkdownPipelineCallback callback, Document file, JObject extensionData = null)
        {
            return TransformContent;
            object TransformContent(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                string result = (string)value;

                if (attribute is HrefAttribute)
                {
                    result = Markup.GetLink((string)value, file, file, callback, errors);
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, callback, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, callback, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is HtmlAttribute)
                {
                    var html = HtmlUtility.TransformLinks((string)value, href => Markup.GetLink(href, file, file, callback, errors));
                    result = HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
                }

                if (attribute is XrefAttribute)
                {
                    // TODO: how to fill xref resolving data besides href
                    result = callback?.XrefMap.Resolve((string)value).Href;
                }

                if (extensionData != null && attributes.Any(attr => attr is XrefPropertyAttribute))
                {
                    extensionData[jsonPath] = result;
                }

                return result;
            }
        }
    }
}
