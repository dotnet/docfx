// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class AttributeTransformer
    {
        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> TransformSDP(
            Context context,
            Document file,
            Action<Document> buildChild)
        {
            return Transform;

            object Transform(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                return TransformContent(context, attribute, value, file, buildChild);
            }
        }

        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> TransformXref(
            Context context,
            Document file,
            Action<Document> buildChild,
            Dictionary<string, Lazy<JValue>> extensionData)
        {
            return TransformXrefSpec;

            object TransformXrefSpec(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                extensionData[jsonPath] = new Lazy<JValue>(() => new JValue(TransformContent(context, attribute, value, file, buildChild)), LazyThreadSafetyMode.PublicationOnly);
                return null;
            }
        }

        private static object TransformContent(Context context, DataTypeAttribute attribute, object value, Document file, Action<Document> buildChild)
        {
            var range = JsonUtility.ToRange(value as IJsonLineInfo);
            if (attribute is HrefAttribute)
            {
                var (error, link, _) = context.DependencyResolver.ResolveLink((string)value, file, file, buildChild, range, forLandingPage: file.FilePath.EndsWith("index.yml"));

                context.Report.Write(file.ToString(), error);
                return link;
            }

            if (attribute is MarkdownAttribute)
            {
                var (errors, html) = MarkdownUtility.ToHtml(
                    (string)value,
                    file,
                    context.DependencyResolver,
                    buildChild,
                    null,
                    key => file.Docset.Template?.GetToken(key),
                    MarkdownPipelineType.Markdown);

                context.Report.Write(file.ToString(), errors);
                return html;
            }

            if (attribute is InlineMarkdownAttribute)
            {
                var (errors, html) = MarkdownUtility.ToHtml(
                    (string)value,
                    file,
                    context.DependencyResolver,
                    buildChild,
                    null,
                    key => file.Docset.Template?.GetToken(key),
                    MarkdownPipelineType.InlineMarkdown);

                context.Report.Write(file.ToString(), errors);
                return html;
            }

            if (attribute is HtmlAttribute)
            {
                var html = HtmlUtility.TransformLinks((string)value, href =>
                {
                    var (error, link, _) = context.DependencyResolver.ResolveLink(href, file, file, buildChild, range, forLandingPage: file.FilePath.EndsWith("index.yml"));

                    context.Report.Write(file.ToString(), error);
                    return link;
                });
                return HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
            }

            if (attribute is XrefAttribute)
            {
                // TODO: how to fill xref resolving data besides href
                var (error, link, _, _) = context.DependencyResolver.ResolveXref((string)value, file, file);
                context.Report.Write(file.ToString(), error);
                return link;
            }

            return value;
        }
    }
}
