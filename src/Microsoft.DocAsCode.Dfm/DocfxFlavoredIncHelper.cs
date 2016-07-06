// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    internal sealed class DocfxFlavoredIncHelper : IDisposable
    {
        private readonly FileCacheLite _cache;
        private readonly Dictionary<string, HashSet<string>> _dependencyCache = new Dictionary<string, HashSet<string>>();

        public static readonly string InlineIncRegexString = @"^\[!INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]";

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new FilePathComparer());
        }

        public string Load(IMarkdownRenderer adapter, string currentPath, string raw, IMarkdownContext context, DfmEngine engine)
        {
            return LoadCore(adapter, currentPath, raw, context, engine);
        }

        private string LoadCore(IMarkdownRenderer adapter, string currentPath, string raw, IMarkdownContext context, DfmEngine engine)
        {
            try
            {
                if (!PathUtility.IsRelativePath(currentPath))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Absolute path \"{currentPath}\" is not supported.", raw);
                }

                var parents = context.GetFilePathStack();
                var originalPath = currentPath;
                context.ReportDependency(originalPath);
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
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Unable to resolve {raw}: Circular dependency found in \"{parent}\"", raw);
                }

                // Add current file path to chain when entering recursion
                parents = parents.Push(currentPath);
                string result;
                HashSet<string> dependency;
                if (!_dependencyCache.TryGetValue(currentPath, out dependency) ||
                    !_cache.TryGet(currentPath, out result))
                {
                    var src = File.ReadAllText(Path.Combine(context.GetBaseFolder(), currentPath));

                    dependency = new HashSet<string>();
                    src = engine.InternalMarkup(src, context.SetFilePathStack(parents).SetDependency(dependency));

                    result = UpdateToHrefFromWorkingFolder(src, currentPath);
                    result = GenerateNodeWithCommentWrapper("INCLUDE", $"Include content from \"{currentPath}\"", result);

                    _cache.Add(currentPath, result);
                    _dependencyCache[currentPath] = dependency;
                }
                context.ReportDependency(
                    from d in dependency
                    select (string)((RelativePath)currentPath + (RelativePath)d - (RelativePath)parent));
                return result;
            }
            catch (Exception e)
            {
                return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Unable to resolve {raw}:{e.Message}", raw);
            }
        }

        private static string GenerateErrorNodeWithCommentWrapper(string tag, string comment, string html)
        {
            Logger.LogError(comment);
            return GenerateNodeWithCommentWrapper("ERROR " + tag, comment, html);
        }

        private static string GenerateNodeWithCommentWrapper(string tag, string comment, string html)
        {
            string escapedTag = StringHelper.Escape(tag);
            return $"<!-- BEGIN {escapedTag}: {StringHelper.Escape(comment)} -->{html}<!--END {escapedTag} -->";
        }

        private static string UpdateToHrefFromWorkingFolder(string html, string filePath)
        {
            return UpdateHtml(html, node => UpdateToHrefFromWorkingFolder(node, filePath));
        }

        private static void UpdateToHrefFromWorkingFolder(HtmlNode html, string filePath)
        {
            foreach (var pair in GetHrefNodes(html))
            {
                var link = pair.Attr;
                if (PathUtility.IsRelativePath(link.Value) && !RelativePath.IsPathFromWorkingFolder(link.Value) && !link.Value.StartsWith("#"))
                {
                    link.Value = ((RelativePath)filePath + (RelativePath)link.Value).GetPathFromWorkingFolder();
                }
            }
        }

        private static List<NodeInfo> GetHrefNodes(HtmlNode html)
        {
            return (from n in html.Descendants()
                    where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                    from attr in n.Attributes
                    where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                    where !string.IsNullOrWhiteSpace(attr.Value)
                    select new NodeInfo(n, attr)).ToList();
        }

        private static string UpdateHtml(string html, Action<HtmlNode> updater)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode;
            updater(node);
            return node.WriteTo();
        }

        private sealed class NodeInfo
        {
            public HtmlNode Node { get; }
            public HtmlAttribute Attr { get; }
            public NodeInfo(HtmlNode node, HtmlAttribute attr)
            {
                Node = node;
                Attr = attr;
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
