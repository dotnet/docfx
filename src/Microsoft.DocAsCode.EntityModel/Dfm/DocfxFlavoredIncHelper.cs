// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using HtmlAgilityPack;
    using MarkdownLite;
    using Utility;

    internal sealed class DocfxFlavoredIncHelper : IDisposable
    {
        private readonly FileCacheLite _cache;

        public static readonly string InlineIncRegexString = @"^\[!INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]";

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new FilePathComparer());
        }

        public string Load(string currentPath, string raw, Stack<string> parents, Func<string, Stack<string>, string> resolver)
        {
            return LoadCore(currentPath, raw, parents, resolver);
        }

        private string LoadCore(string currentPath, string raw, Stack<string> parents, Func<string, Stack<string>, string> resolver)
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

            var originalPath = currentPath;
            string parent = string.Empty;
            if (parents == null) parents = new Stack<string>();

            // Update currentPath to be referencing to sourcePath
            else if (parents.Count > 0)
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
            parents.Push(currentPath);
            try
            {
                if (!_cache.TryGet(currentPath, out result))
                {
                    var src = File.ReadAllText(currentPath);

                    src = resolver(src, parents);

                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(src);
                    var node = htmlDoc.DocumentNode;

                    // If current content is not the root one, update href to root
                    if (parents.Count > 1)
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

            // Remove current file path when leaving recusion
            parents.Pop();
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
