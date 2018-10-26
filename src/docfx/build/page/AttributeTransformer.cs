// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class AttributeTransformer
    {
        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> Transform(
            List<Error> errors,
            AttributeTransformerCallback callback,
            Document file,
            JObject extensionData = null)
        {
            return TransformContent;
            object TransformContent(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                string result = (string)value;

                if (attribute is HrefAttribute)
                {
                    result = GetLink((string)value, file, file, callback, errors);
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, ReadFileDelegate, GetLinkDelegate, ResolveXrefDelegate, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, ReadFileDelegate, GetLinkDelegate, ResolveXrefDelegate, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    result = html;
                }

                if (attribute is HtmlAttribute)
                {
                    var html = HtmlUtility.TransformLinks((string)value, href => GetLink(href, file, file, callback, errors));
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

                (string content, object file) ReadFileDelegate(string path, object relativeTo)
                    => ReadFile(path, relativeTo, callback?.DependencyMap, errors);

                string GetLinkDelegate(string path, object relativeTo, object resultRelativeTo)
                    => GetLink(path, relativeTo, resultRelativeTo, callback, errors);

                XrefSpec ResolveXrefDelegate(string uid)
                    => ResolveXref(uid, callback?.XrefMap);
            }
        }

        public static (string content, object file) ReadFile(string path, object relativeTo, DependencyMapBuilder dependencyMapBuilder, List<Error> errors)
        {
            Debug.Assert(relativeTo is Document);

            var (error, content, child) = ((Document)relativeTo).TryResolveContent(path);

            errors.AddIfNotNull(error);

            dependencyMapBuilder?.AddDependencyItem((Document)relativeTo, child, DependencyType.Inclusion);

            return (content, child);
        }

        public static string GetLink(string path, object relativeTo, object resultRelativeTo, AttributeTransformerCallback callback, List<Error> errors)
        {
            Debug.Assert(relativeTo is Document);
            Debug.Assert(resultRelativeTo is Document);

            var self = (Document)relativeTo;
            var (error, link, fragment, child) = self.TryResolveHref(path, (Document)resultRelativeTo);
            errors.AddIfNotNull(error);

            if (child != null && callback?.BuildChild != null)
            {
                callback?.BuildChild(child);
                callback?.DependencyMap?.AddDependencyItem(self, child, HrefUtility.FragmentToDependencyType(fragment));
            }

            callback?.BookmarkValidator?.AddBookmarkReference(self, child ?? self, fragment);

            return link;
        }

        public static XrefSpec ResolveXref(string uid, XrefMap xrefMap)
        {
            return xrefMap?.Resolve(uid);
        }
    }
}
