// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal static class DocsValidationExtension
    {
        public static MarkdownPipelineBuilder UseDocsValidation(
            this MarkdownPipelineBuilder builder,
            MarkdownEngine markdownEngine,
            ContentValidator contentValidator,
            Func<MonikerList> getFileLevelMonikers,
            Func<string?> getCanonicalVersion)
        {
            return builder.Use(document =>
            {
                var currentFile = ((SourceInfo)InclusionContext.File).File;
                if (currentFile.Format != FileFormat.Markdown)
                {
                    return;
                }

                var documentNodes = new List<ContentNode>();
                var codeBlockNodes = new List<(bool isInclude, CodeBlockItem codeBlockItem)>();

                var canonicalVersion = getCanonicalVersion();
                var fileLevelMoniker = getFileLevelMonikers();

                var zonePivotUsages = new List<SourceInfo<string>>();

                document.Visit(node =>
                {
                    // Skip leaf triple colon nodes
                    if (node is TripleColonBlock tripleColon)
                    {
                        if (tripleColon.Extension is ImageExtension || tripleColon.Extension is VideoExtension || tripleColon.Extension is CodeExtension)
                        {
                            return true;
                        }

                        // Build zones for validation
                        BuildZonePivotUsages(tripleColon, zonePivotUsages);
                    }

                    var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, node.GetZoneLevelMonikers());

                    BuildHeadingNodes(node, markdownEngine, documentNodes, isCanonicalVersion);

                    BuildCodeBlockNodes(node, codeBlockNodes, isCanonicalVersion);

                    return false;
                });

                contentValidator.ValidateHeadings(currentFile, documentNodes);
                contentValidator.ValidateZonePivots(currentFile, zonePivotUsages);

                foreach (var (_, codeBlockItem) in codeBlockNodes)
                {
                    contentValidator.ValidateCodeBlock(currentFile, codeBlockItem);
                }
            });
        }

        private static void BuildHeadingNodes(
            MarkdownObject node,
            MarkdownEngine markdownEngine,
            List<ContentNode> documentNodes,
            bool isCanonicalVersion)
        {
            ContentNode? documentNode = null;

            switch (node)
            {
                case HeadingBlock headingBlock:
                    var headingNode = CreateValidationNode<Heading>(isCanonicalVersion, headingBlock);

                    headingNode.Level = headingBlock.Level;
                    headingNode.Content = GetHeadingContent(headingBlock); // used for reporting
                    headingNode.HeadingChar = headingBlock.HeaderChar;
                    headingNode.RenderedPlainText = markdownEngine.ToPlainText(headingBlock); // used for validation
                    headingNode.IsVisible = MarkdigUtility.IsVisible(headingBlock);

                    documentNode = headingNode;
                    break;

                case LeafBlock leafBlock:
                    var contentNode = CreateValidationNode<ContentNode>(isCanonicalVersion, leafBlock);
                    contentNode.IsVisible = MarkdigUtility.IsVisible(leafBlock);
                    documentNode = contentNode;
                    break;
            }

            if (documentNode != null)
            {
                documentNodes.Add(documentNode);
            }
        }

        private static void BuildCodeBlockNodes(
            MarkdownObject node,
            List<(bool IsInclude, CodeBlockItem codeBlockItem)> codeBlockItemList,
            bool isCanonicalVersion)
        {
            CodeBlockItem? codeBlockItem = null;

            switch (node)
            {
                case FencedCodeBlock fencedCodeBlock:
                    codeBlockItem = CreateValidationNode<CodeBlockItem>(isCanonicalVersion, node);

                    codeBlockItem.Type = CodeBlockTypeEnum.FencedCodeBlock;
                    codeBlockItem.Info = fencedCodeBlock.Info;
                    codeBlockItem.Arguments = fencedCodeBlock.Arguments;
                    codeBlockItem.IsOpen = fencedCodeBlock.IsOpen;
                    codeBlockItem.LineCount = GetFencedCodeBlockNetLineCount(fencedCodeBlock);
                    break;

                case YamlFrontMatterBlock _:
                    break;

                case CodeBlock codeBlock:
                    codeBlockItem = CreateValidationNode<CodeBlockItem>(isCanonicalVersion, codeBlock);
                    codeBlockItem.Type = CodeBlockTypeEnum.CodeBlock;
                    break;

                default:
                    break;
            }

            if (codeBlockItem != null)
            {
                codeBlockItemList.Add((node.IsInclude(), codeBlockItem));
            }
        }

        private static void BuildZonePivotUsages(TripleColonBlock tripleColon, List<SourceInfo<string>> usages)
        {
            if (tripleColon.Extension is ZoneExtension && tripleColon.Attributes.TryGetValue("pivot", out var pivotId))
            {
                usages.AddRange(pivotId.Split(",").Select(p => new SourceInfo<string>(p.Trim(), tripleColon.GetSourceInfo())));
            }
        }

        private static int GetFencedCodeBlockNetLineCount(FencedCodeBlock fencedCodeBlock)
        {
            var netLineCount = 0;

            for (var i = 0; i < fencedCodeBlock.Lines.Count; i++)
            {
                var temSlice = fencedCodeBlock.Lines.Lines[i].Slice;
                temSlice.Trim();

                if (!temSlice.IsEmpty)
                {
                    netLineCount++;
                }
            }

            return netLineCount;
        }

        private static bool IsCanonicalVersion(string? canonicalVersion, MonikerList fileLevelMonikerList, MonikerList zoneLevelMonikerList)
        {
            if (zoneLevelMonikerList.HasMonikers)
            {
                return zoneLevelMonikerList.IsCanonicalVersion(canonicalVersion);
            }

            return fileLevelMonikerList.IsCanonicalVersion(canonicalVersion);
        }

        private static T CreateValidationNode<T>(bool isCanonicalVersion, MarkdownObject markdownNode)
            where T : ValidationNode, new()
        {
            return new T()
            {
                IsCanonicalVersion = isCanonicalVersion,
                ParentSourceInfoList = markdownNode.GetInclusionStack(),
                Zone = markdownNode.GetZone(),
                Monikers = markdownNode.GetZoneLevelMonikers().ToList(),
                ZonePivots = markdownNode.GetZonePivots(),
                SourceInfo = markdownNode.GetSourceInfo(),
                TabbedConceptualHeader = markdownNode.GetTabId(),
            };
        }

        private static string GetHeadingContent(HeadingBlock headingBlock)
        {
            if (headingBlock.Inline is null || !headingBlock.Inline.Any())
            {
                return "";
            }

            return GetContainerInlineContent(headingBlock.Inline);
            static string GetContainerInlineContent(ContainerInline containerInline)
            {
                var content = new StringBuilder();
                var child = containerInline.FirstChild;
                while (child != null)
                {
                    if (child is LiteralInline childLiteralInline)
                    {
                        content.Append(childLiteralInline.Content.Text, childLiteralInline.Content.Start, childLiteralInline.Content.Length);
                    }

                    if (child is HtmlInline childHtmlInline)
                    {
                        content.Append(childHtmlInline.Tag);
                    }

                    if (child is ContainerInline childContainerInline)
                    {
                        content.Append(GetContainerInlineContent(childContainerInline));
                    }

                    child = child.NextSibling;
                }

                return content.ToString();
            }
        }
    }
}
