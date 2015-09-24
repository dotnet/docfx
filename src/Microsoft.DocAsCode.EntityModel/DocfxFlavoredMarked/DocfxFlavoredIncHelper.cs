// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using HtmlAgilityPack;
    using MarkdownLite;
    using Utility;

    internal sealed class DocfxFlavoredIncHelper : IDisposable
    {
        private readonly FileCacheLite _cache;

        public static readonly string InlineIncRegexString = @"^\[!INC\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]";

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new FilePathComparer());
        }

        public string Load(string currentPath, string title, string value, string raw, Stack<string> parents, string src, Func<string, Stack<string>, string> resolver, MarkdownNodeType nodeType, DocfxFlavoredOptions options)
        {
            // If currentPath is not set, unable to resolve inc syntax
            if (string.IsNullOrEmpty(currentPath)) return resolver(src, parents);
            currentPath = FileExtensions.MakeRelativePath(Environment.CurrentDirectory, currentPath);
            return LoadCore((RelativePath)currentPath, title, value, raw, parents, src, resolver, nodeType, options);
        }

        /// <summary>
        /// The actual recursive function to load included files
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="title"></param>
        /// <param name="value"></param>
        /// <param name="parents"></param>
        /// <param name="src"></param>
        /// <param name="resolver"></param>
        /// <param name="nodeType"></param>
        /// <param name="visited"></param>
        /// <param name="isRoot">If current content is ROOT content, href will not be updated</param>
        /// <returns></returns>
        private string LoadCore(RelativePath currentPath, string title, string value, string raw, Stack<string> parents, string src, Func<string, Stack<string>, string> resolver, MarkdownNodeType nodeType, DocfxFlavoredOptions options)
        {
            if (string.IsNullOrEmpty(src) && !IsRelativePath(currentPath))
                return GenerateNodeWithCommentWrapper("ERROR INC", $"Invalid file path {currentPath}", raw);

            var originalPath = currentPath;
            string parent = string.Empty;
            if (parents == null) parents = new Stack<string>();
            // Update currentPath to be referencing to sourcePath
            else if (parents.Count > 0)
            {
                parent = parents.Peek();
                currentPath = currentPath.BasedOn((RelativePath)parent);
            }

            if (parents.Contains(currentPath, FilePathComparer.OSPlatformSensitiveComparer))
            {
                var incSyntax = string.IsNullOrEmpty(title) ? $"[!inc[{value}]({currentPath})]" : $"[!inc[{value}]({currentPath} '{title}')]";
                return GenerateNodeWithCommentWrapper("ERROR INC", $"Unable to resolve \"{incSyntax}\": Circular dependency found in \"{parent}\"", raw);
            }

            string result = string.Empty;
            
            // Add current file path to chain when entering recursion
            parents.Push(currentPath);
            try
            {
                if (!_cache.TryGet(currentPath, out result))
                {
                    if (src == null)
                        src = File.ReadAllText(currentPath);

                    src = resolver(src, parents);

                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(src);
                    var node = htmlDoc.DocumentNode;

                    if (nodeType == MarkdownNodeType.Block)
                    {
                        ReplaceNodes(node, "//inc", (element) =>
                        {
                            // For each include 
                            var eleSrc = element.GetAttributeValue("src", string.Empty);
                            var eleTitle = element.GetAttributeValue("title", string.Empty);
                            var eleValue = element.InnerText;
                            if (IsRelativePath(eleSrc))
                            {
                                var parsed = LoadCore((RelativePath)eleSrc, eleTitle, eleValue, element.OuterHtml, parents, null, resolver, nodeType, options);
                                return GenerateNodeWithCommentWrapper("INC", $"Include content from \"{eleSrc}\"", parsed);
                            }
                            else
                            {
                                return GenerateNodeWithCommentWrapper("ERROR INC", $"Absolute path \"{eleSrc}\" is not supported.", element.OuterHtml);
                            }
                        });
                    }

                    // If current content is not the root one, update href to root
                    if (parents.Count > 1)
                        UpdateHref(node, originalPath);

                    result = node.WriteTo();
                    if (nodeType == MarkdownNodeType.Inline)
                    {
                        result = GenerateNodeWithCommentWrapper("INC", $"Include content from \"{currentPath}\"", result);
                    }
                }
            }
            catch (Exception e)
            {
                result = GenerateNodeWithCommentWrapper("ERROR INC", $"Unable to resolve \"[!inc[{value}]({currentPath} '{title}')]\":{e.Message}", raw);
            }

            _cache.Add(currentPath, result);

            // Remove current file path when leaving recusion
            parents.Pop();
            return result;
        }

        private static void ReplaceNodes(HtmlNode documentNode, string selector, Func<HtmlNode, string> updater)
        {
            var nodes = documentNode.SelectNodes(selector);
            if (nodes == null) return;
            foreach(var node in nodes)
            {
                var replacer = updater(node);

                var innerDoc = new HtmlDocument();
                innerDoc.LoadHtml(replacer);

                documentNode.InsertBefore(innerDoc.DocumentNode, node);
                node.Remove();
            }
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
            if (IsRelativePath(href))
                node.SetAttributeValue(attributeName, RebaseHref(href, filePath, string.Empty));
        }

        private static bool IsRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) return false;

            return !Path.IsPathRooted(path);
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
