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
            PageCallback callback,
            GitCommitProvider gitCommitProvider,
            JObject extensionData = null)
        {
            return TransformContent;
            object TransformContent(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                string result = (string)value;

                if (attribute is HrefAttribute)
                {
                    result = Resolve.GetLink((string)value, file, file, errors, callback?.BuildChild, callback?.DependencyMapBuilder, callback?.BookmarkValidator);
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, ReadFileDelegate, GetLinkDelegate, ResolveXrefDelegate, null, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, ReadFileDelegate, GetLinkDelegate, ResolveXrefDelegate, null, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is HtmlAttribute)
                {
                    var html = HtmlUtility.TransformLinks((string)value, href => Resolve.GetLink(href, file, file, errors, callback?.BuildChild, callback?.DependencyMapBuilder, callback?.BookmarkValidator));
                    result = HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
                }

                if (attribute is XrefAttribute)
                {
                    // TODO: how to fill xref resolving data besides href
                    result = Resolve.ResolveXref((string)value, callback?.XrefMap, file, callback?.DependencyMapBuilder)?.Href;
                }

                if (extensionData != null && attributes.Any(attr => attr is XrefPropertyAttribute))
                {
                    extensionData[jsonPath] = result;
                }

                return result;

                (string content, object file) ReadFileDelegate(string path, object relativeTo)
                    => Resolve.ReadFile(path, relativeTo, errors, callback?.DependencyMapBuilder, gitCommitProvider);

                string GetLinkDelegate(string path, object relativeTo, object resultRelativeTo)
                    => Resolve.GetLink(path, relativeTo, resultRelativeTo, errors, callback?.BuildChild, callback?.DependencyMapBuilder, callback?.BookmarkValidator);

                XrefSpec ResolveXrefDelegate(string uid, string moniker)
                    => Resolve.ResolveXref(uid, callback?.XrefMap, file, callback?.DependencyMapBuilder, moniker);
            }
        }
    }
}
