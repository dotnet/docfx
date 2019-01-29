// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class AttributeTransformer
    {
        public static Func<IEnumerable<DataTypeAttribute>, object, string, (List<Error> error, object content)> TransformSDP(
            Context context,
            Document file,
            Action<Document> buildChild)
        {
            return Transform;

            (List<Error> error, object content) Transform(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                var (errors, content) = TransformContent(context, attribute, value, file, buildChild);

                return (errors.WithFile(file.ToString()), content);
            }
        }

        public static Func<IEnumerable<DataTypeAttribute>, object, string, (List<Error> errors, object content)> TransformXref(
            Context context,
            Document file,
            Action<Document> buildChild,
            Dictionary<string, Lazy<(List<Error> errors, JValue jValue)>> extensionData)
        {
            return TransformXrefSpec;

            (List<Error> errors, object content) TransformXrefSpec(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                extensionData[jsonPath] = new Lazy<(List<Error> errors, JValue jValue)>(
                    () =>
                    {
                        var (errors, content) = TransformContent(context, attribute, value, file, buildChild);
                        return (errors.WithFile(file.ToString()), new JValue(content));
                    },
                    LazyThreadSafetyMode.PublicationOnly);

                return (new List<Error>(), null);
            }
        }

        private static (List<Error> errors, object content) TransformContent(Context context, DataTypeAttribute attribute, object value, Document file, Action<Document> buildChild)
        {
            if (attribute is HrefAttribute)
            {
                var (errors, link, _) = context.DependencyResolver.ResolveLink((string)value, file, file, buildChild);
                return (errors, link);
            }

            if (attribute is MarkdownAttribute)
            {
                var (html, markup) = MarkdownUtility.ToHtml((string)value, file, context.DependencyResolver, buildChild, null, MarkdownPipelineType.Markdown);
                return (markup.Errors, html);
            }

            if (attribute is InlineMarkdownAttribute)
            {
                var (html, markup) = MarkdownUtility.ToHtml((string)value, file, context.DependencyResolver, buildChild, null, MarkdownPipelineType.InlineMarkdown);
                return (markup.Errors, html);
            }

            if (attribute is HtmlAttribute)
            {
                var htmlErrors = new List<Error>();
                var html = HtmlUtility.TransformLinks((string)value, href =>
                {
                    var (errors, link, _) = context.DependencyResolver.ResolveLink(href, file, file, buildChild);
                    htmlErrors.AddRange(errors);
                    return link;
                });
                return (htmlErrors, HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml);
            }

            if (attribute is XrefAttribute)
            {
                // TODO: how to fill xref resolving data besides href
                var (errors, link, _, _) = context.DependencyResolver.ResolveXref((string)value, file, file);
                return (errors, link);
            }

            return (new List<Error>(),  value);
        }
    }
}
