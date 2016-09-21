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
        private readonly FileCacheLite _fallbackCache;
        private readonly Dictionary<string, HashSet<string>> _dependencyCache = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> _fallbackDependencyCache = new Dictionary<string, HashSet<string>>();

        public static readonly string InlineIncRegexString = @"^\[!INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]";

        public DocfxFlavoredIncHelper()
        {
            _cache = new FileCacheLite(new FilePathComparer());
            _fallbackCache = new FileCacheLite(new FilePathComparer());
        }

        public string Load(IMarkdownRenderer adapter, string currentPath, string raw, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            return LoadCore(adapter, currentPath, raw, sourceInfo, context, engine);
        }

        private string LoadCore(IMarkdownRenderer adapter, string currentPath, string raw, SourceInfo sourceInfo, IMarkdownContext context, DfmEngine engine)
        {
            try
            {
                if (!PathUtility.IsRelativePath(currentPath))
                {
                    return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Absolute path \"{currentPath}\" is not supported.", raw);
                }

                // Always report original include file dependency
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
                    var originalIncludeFilePath = Path.Combine(context.GetBaseFolder(), currentPath);
                    var actualIncludeFilePath = originalIncludeFilePath;
                    var hitFallback = false;
                    if (!File.Exists(originalIncludeFilePath))
                    {
                        // Caution: fallback folder should have order. Such as en-us -> zh-tw -> zh-cn -> ...
                        var fallbackFolders = context.GetFallbackFolders();
                        foreach (var folder in fallbackFolders)
                        {
                            var fallbackFilePath = Path.Combine(folder, currentPath);
                            var fallbackFileRelativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(originalIncludeFilePath), fallbackFilePath);
                            context.ReportDependency(fallbackFileRelativePath); // All the high priority fallback files should be reported to the dependency.
                            if (!_fallbackDependencyCache.TryGetValue(fallbackFileRelativePath, out dependency) || !_fallbackCache.TryGet(fallbackFileRelativePath, out result))
                            {
                                if (File.Exists(fallbackFilePath))
                                {
                                    actualIncludeFilePath = fallbackFilePath;
                                    hitFallback = true;
                                    break;
                                }
                            }
                            else
                            {
                                // If we can get fallback information for both file and dependency, then it means fallback cache hit, just return.
                                context.ReportDependency(
                                    from d in dependency
                                    select (string)((RelativePath)currentPath + (RelativePath)d - (RelativePath)parent));
                                return result;
                            }
                        }

                        if (!hitFallback)
                        {
                            if (fallbackFolders.Count > 0)
                            {
                                throw new FileNotFoundException($"Couldn't find file {originalIncludeFilePath}. Fallback folders: {string.Join(",", fallbackFolders)}", originalIncludeFilePath);
                            }
                            throw new FileNotFoundException($"Couldn't find file {originalIncludeFilePath}.", originalIncludeFilePath);
                        }
                    }

                    var src = File.ReadAllText(actualIncludeFilePath);
                    dependency = new HashSet<string>();
                    src = engine.InternalMarkup(src, context.SetFilePathStack(parents).SetDependency(dependency).SetIsInclude());

                    result = UpdateToHrefFromWorkingFolder(src, currentPath);
                    result = GenerateNodeWithCommentWrapper("INCLUDE", $"Include content from \"{currentPath}\"", result);
                    if (!hitFallback)
                    {
                        _cache.Add(currentPath, result);
                        _dependencyCache[currentPath] = dependency;
                    }
                    else
                    {
                        var fallbackFileRelativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(originalIncludeFilePath), actualIncludeFilePath);
                        _fallbackCache.Add(fallbackFileRelativePath, result);
                        _fallbackDependencyCache[fallbackFileRelativePath] = dependency;
                    }
                }
                context.ReportDependency(
                    from d in dependency
                    select (string)((RelativePath)currentPath + (RelativePath)d - (RelativePath)parent));
                return result;
            }
            catch (Exception e)
            {
                return GenerateErrorNodeWithCommentWrapper("INCLUDE", $"Unable to resolve {raw}:{e.Message}. at line {sourceInfo.LineNumber}.", raw);
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
            _fallbackCache.Dispose();
        }
    }
}
