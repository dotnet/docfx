// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using HtmlAgilityPack;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    internal sealed class DocfxFlavoredIncHelper : IDisposable
    {
        private readonly FileCacheLite _cache;

        public static readonly string InlineIncRegexString = @"^\[!INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]";

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new FilePathComparer());
        }

        public string Load(DfmRendererAdapter adapter, string currentPath, string raw, IMarkdownContext context, Func<string, IMarkdownContext, string> resolver)
        {
            return LoadCore(adapter, currentPath, raw, context, resolver);
        }

        private string LoadCore(DfmRendererAdapter adapter, string currentPath, string raw, IMarkdownContext context, Func<string, IMarkdownContext, string> resolver)
        {
            if (!PathUtility.IsRelativePath(currentPath))
            {
                if (!Path.IsPathRooted(currentPath))
                {
                    return GenerateNodeWithCommentWrapper("ERROR INCLUDE", $"Absolute path \"{currentPath}\" is not supported.", raw);
                }
                else
                    currentPath = PathUtility.MakeRelativePath(Environment.CurrentDirectory, currentPath);
            }
            var parents = adapter.GetFilePathStack(context);
            var originalPath = currentPath;
            string parent = string.Empty;
            if (parents == null) parents = ImmutableStack<string>.Empty;

            // Update currentPath to be referencing to sourcePath
            else if (!parents.IsEmpty)
            {
                parent = parents.Peek();
                currentPath = ((RelativePath)currentPath).BasedOn((RelativePath)parent);
            }

            if (parents.Contains(currentPath, FilePathComparer.OSPlatformSensitiveComparer))
            {
                return GenerateNodeWithCommentWrapper("ERROR INCLUDE", $"Unable to resolve {raw}: Circular dependency found in \"{parent}\"", raw);
            }

            string result = string.Empty;

            // Add current file path to chain when entering recursion
            parents = parents.Push(currentPath);
            try
            {
                if (!_cache.TryGet(currentPath, out result))
                {
                    var src = File.ReadAllText(currentPath);

                    src = resolver(src, adapter.SetFilePathStack(context, parents));

                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(src);
                    var node = htmlDoc.DocumentNode;

                    // If current content is not the root one, update href to root
                    if (parents.Count() > 1)
                        UpdateHref(node, originalPath);

                    result = node.WriteTo();
                    result = GenerateNodeWithCommentWrapper("INCLUDE", $"Include content from \"{currentPath}\"", result);
                }
            }
            catch (Exception e)
            {
                result = GenerateNodeWithCommentWrapper("ERROR INCLUDE", $"Unable to resolve {raw}:{e.Message}", raw);
            }

            _cache.Add(currentPath, result);

            return result;
        }

        private static string GenerateNodeWithCommentWrapper(string tag, string comment, string html)
        {
            string escapedTag = StringHelper.Escape(tag);
            return $"<!-- BEGIN {escapedTag}: {StringHelper.Escape(comment)} -->{html}<!--END {escapedTag} -->";
        }

        private static void UpdateHref(HtmlNode node, string filePath)
        {
            var selector = node.SelectNodes("//img");
            if (selector != null)
            {
                foreach (var element in selector)
                {
                    UpdateSingleHref(element, "src", filePath);
                }
            }

            selector = node.SelectNodes("//a|//link|//script");
            if (selector != null)
            {
                foreach (var element in selector)
                {
                    UpdateSingleHref(element, "href", filePath);
                }
            }
        }

        private static void UpdateSingleHref(HtmlNode node, string attributeName, string filePath)
        {
            var href = node.GetAttributeValue(attributeName, string.Empty);
            if (PathUtility.IsRelativePath(href))
                node.SetAttributeValue(attributeName, RebaseHref(href, filePath, string.Empty));
        }

        private static string RebaseHref(string refPath, string source, string target)
        {
            var originalPath = (RelativePath)refPath;

            var from = (RelativePath)source;
            var to = (RelativePath)target;
            var rebasedPath = originalPath.Rebase(from, to);
            return rebasedPath;
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
