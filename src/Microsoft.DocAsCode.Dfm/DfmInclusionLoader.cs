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
    using Microsoft.DocAsCode.Plugins;

    internal class DfmInclusionLoader : IDisposable
    {
        private readonly FileCacheLite _cache;
        private readonly Dictionary<string, HashSet<string>> _dependencyCache = new Dictionary<string, HashSet<string>>();

        public DfmInclusionLoader()
        {
            _cache = new FileCacheLite(new FilePathComparer());
        }

        public virtual string Load(IMarkdownRenderer adapter, string currentPath, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            return LoadCore(adapter, currentPath, sourceInfo, context, engine);
        }

        private string LoadCore(IMarkdownRenderer adapter, string currentPath, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            try
            {
                if (!PathUtility.IsRelativePath(currentPath))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Absolute path \"{currentPath}\" is not supported.", sourceInfo);
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
                    currentPath = ((RelativePath)currentPath).BasedOn((RelativePath)parent);
                }

                if (parents.Contains(currentPath, FilePathComparer.OSPlatformSensitiveRelativePathComparer))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Circular dependency found in \"{parent}\"", sourceInfo);
                }

                // Add current file path to chain when entering recursion
                parents = parents.Push(currentPath);
                if (!_dependencyCache.TryGetValue(currentPath, out HashSet<string> dependency) ||
                    !_cache.TryGet(currentPath, out string result))
                {
                    var src = GetIncludedContent(originalRelativePath, context);
                    dependency = new HashSet<string>();
                    src = new DfmEngine(engine).InternalMarkup(src, context.SetFilePathStack(parents).SetDependency(dependency).SetIsInclude());

                    result = UpdateToHrefFromWorkingFolder(src, currentPath);
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
                return GenerateErrorNodeWithCommentWrapper("INCLUDE", e.Message, sourceInfo);
            }
        }

        protected virtual string GetIncludedContent(string filePath, IMarkdownContext context)
        {
            var filePathWithStatus = DfmFallbackHelper.GetFilePathWithFallback(filePath, context);
            return EnvironmentContext.FileAbstractLayer.ReadAllText(filePathWithStatus.Item1);
        }

        private static string GenerateErrorNodeWithCommentWrapper(string tag, string comment, SourceInfo sourceInfo)
        {
            Logger.LogWarning($"Unable to resolve \"{sourceInfo.Markdown}\" at line {sourceInfo.LineNumber}: " + comment, code: WarningCodes.Markdown.InvalidInclude);
            return GenerateNodeWithCommentWrapper("ERROR " + tag, $"Unable to resolve {sourceInfo.Markdown}: {comment}", StringHelper.Escape(sourceInfo.Markdown));
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
                if (PathUtility.IsRelativePath(link.Value) && !RelativePath.IsPathFromWorkingFolder(link.Value) && !link.Value.StartsWith("#", StringComparison.Ordinal))
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
