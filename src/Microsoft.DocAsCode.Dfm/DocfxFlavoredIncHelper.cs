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

    using TypeForwardedToFilePathComparer = Microsoft.DocAsCode.Common.FilePathComparer;
    using TypeForwardedToPathUtility = Microsoft.DocAsCode.Common.PathUtility;
    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;

    internal sealed class DocfxFlavoredIncHelper : IDisposable
    {
        private readonly FileCacheLite _cache;
        private readonly Dictionary<string, HashSet<string>> _dependencyCache = new Dictionary<string, HashSet<string>>();

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new TypeForwardedToFilePathComparer());
        }

        public string Load(IMarkdownRenderer adapter, string currentPath, string raw, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            return LoadCore(adapter, currentPath, raw, sourceInfo, context, engine);
        }

        private string LoadCore(IMarkdownRenderer adapter, string currentPath, string raw, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            try
            {
                if (!TypeForwardedToPathUtility.IsRelativePath(currentPath))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Absolute path \"{currentPath}\" is not supported.", raw, sourceInfo);
                }

                // Always report original include file dependency
                var originalRelativePath = currentPath;
                context.ReportDependency(currentPath);

                var parents = context.GetFilePathStack();
                string parent = string.Empty;
                if (parents == null) parents = ImmutableStack<string>.Empty;

                // Update currentPath to be referencing to sourcePath
                else if (!parents.IsEmpty)
                {
                    parent = parents.Peek();
                    currentPath = ((TypeForwardedToRelativePath)currentPath).BasedOn((TypeForwardedToRelativePath)parent);
                }

                if (parents.Contains(currentPath, TypeForwardedToFilePathComparer.OSPlatformSensitiveComparer))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Unable to resolve {raw}: Circular dependency found in \"{parent}\"", raw, sourceInfo);
                }

                // Add current file path to chain when entering recursion
                parents = parents.Push(currentPath);
                string result;
                HashSet<string> dependency;
                if (!_dependencyCache.TryGetValue(currentPath, out dependency) ||
                    !_cache.TryGet(currentPath, out result))
                {
                    var filePathWithStatus = DfmFallbackHelper.GetFilePathWithFallback(originalRelativePath, context);
                    var src = File.ReadAllText(filePathWithStatus.Item1);
                    dependency = new HashSet<string>();
                    src = engine.InternalMarkup(src, context.SetFilePathStack(parents).SetDependency(dependency).SetIsInclude());

                    result = UpdateToHrefFromWorkingFolder(src, currentPath);
                    result = GenerateNodeWithCommentWrapper("INCLUDE", $"Include content from \"{currentPath}\"", result);
                    _cache.Add(currentPath, result);
                    _dependencyCache[currentPath] = dependency;
                }
                context.ReportDependency(
                    from d in dependency
                    select (string)((TypeForwardedToRelativePath)currentPath + (TypeForwardedToRelativePath)d - (TypeForwardedToRelativePath)parent));
                return result;
            }
            catch (Exception e)
            {
                return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Unable to resolve {raw}:{e.Message}", raw, sourceInfo);
            }
        }

        private static string GenerateErrorNodeWithCommentWrapper(string tag, string comment, string html, SourceInfo sourceInfo)
        {
            Logger.LogError(comment + $"at line {sourceInfo.LineNumber}.");
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
                if (TypeForwardedToPathUtility.IsRelativePath(link.Value) && !TypeForwardedToRelativePath.IsPathFromWorkingFolder(link.Value) && !link.Value.StartsWith("#"))
                {
                    link.Value = ((TypeForwardedToRelativePath)filePath + (TypeForwardedToRelativePath)link.Value).GetPathFromWorkingFolder();
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
