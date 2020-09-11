// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlReaderWriter;
using Markdig;
using Markdig.Extensions.Tables;
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
                var currentFile = (FilePath)InclusionContext.File;
                if (currentFile.Format != FileFormat.Markdown)
                {
                    return;
                }

                var documentNodes = new List<ContentNode>();
                var inclusionDocumentNodes = new Dictionary<FilePath, List<ContentNode>>();
                var codeBlockNodes = new List<(bool isInclude, CodeBlockItem codeBlockItem)>();

                var canonicalVersion = getCanonicalVersion();
                var fileLevelMoniker = getFileLevelMonikers();

                document.Visit(node =>
                {
                    // Skip leaf triple colon nodes
                    if (node is TripleColonBlock tripleColon)
                    {
                        if (tripleColon.Extension is ImageExtension || tripleColon.Extension is VideoExtension || tripleColon.Extension is CodeExtension)
                        {
                            return true;
                        }
                    }

                    var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, node.GetZoneLevelMonikers());

                    BuildHeadingNodes(node, markdownEngine, documentNodes, inclusionDocumentNodes, isCanonicalVersion);

                    BuildCodeBlockNodes(node, codeBlockNodes, isCanonicalVersion);

                    return false;
                });

                contentValidator.ValidateHeadings(currentFile, documentNodes, false);
                foreach (var (inclusion, inclusionNodes) in inclusionDocumentNodes)
                {
                    contentValidator.ValidateHeadings(inclusion, inclusionNodes, true);
                }

                foreach (var (isInclude, codeBlockItem) in codeBlockNodes)
                {
                    contentValidator.ValidateCodeBlock(currentFile, codeBlockItem, isInclude);
                }
            });
        }

        public static bool IsInlineImage(this MarkdownObject node)
        {
            switch (node)
            {
                case Inline inline:
                    return inline.IsInlineImage();
                case HtmlBlock htmlBlock:
                    return htmlBlock.IsInlineImage();
                default:
                    return false;
            }
        }

        private static bool IsInlineImage(this Inline node)
        {
            switch (node)
            {
                case LinkInline linkInline when linkInline.IsImage:
                case TripleColonInline tripleColonInline when tripleColonInline.Extension is ImageExtension:
                case HtmlInline htmlInline when htmlInline.Tag.StartsWith("<img", StringComparison.InvariantCultureIgnoreCase):
                    for (MarkdownObject current = node, parent = node.Parent; current != null;)
                    {
                        if (parent is ContainerInline containerInline)
                        {
                            foreach (var child in containerInline)
                            {
                                if (child != current && child.IsVisible())
                                {
                                    return true;
                                }
                            }
                            current = parent;
                            parent = containerInline.Parent;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return node.GetPathToRootExclusive().Any(o => o is TableCell);
                default:
                    return false;
            }
        }

        private static bool IsInlineImage(this HtmlBlock node)
        {
            var stack = new Stack<(HtmlToken? token, int elementCount, bool hasImg)>();
            stack.Push((null, 0, false));
            var reader = new HtmlReader(node.Lines.ToString());
            while (reader.Read(out var token))
            {
                var top = stack.Pop();
                switch (token.Type)
                {
                    case HtmlTokenType.StartTag:
                        top.elementCount += 1;
                        top.hasImg |= token.NameIs("img");
                        stack.Push(top);
                        if (!token.IsSelfClosing && !IsStandardSelfClosingTag(token))
                        {
                            stack.Push((token, 0, false));
                        }
                        break;
                    case HtmlTokenType.EndTag:
                        if (!top.token.HasValue || !top.token.Value.NameIs(token.Name.Span))
                        {
                            // Invalid HTML structure, should throw warning
                            stack.Push(top);
                        }
                        else
                        {
                            if (top.hasImg && top.elementCount > 1)
                            {
                                return true;
                            }
                        }
                        break;
                    case HtmlTokenType.Text:
                        top.elementCount += 1;
                        stack.Push(top);
                        break;
                    default:
                        stack.Push(top);
                        break;
                }
            }

            // Should check if all tags are closed properly and throw warning if not
            return false;

            bool IsStandardSelfClosingTag(HtmlToken token)
            {
                return token.NameIs("area") || token.NameIs("base") || token.NameIs("br") || token.NameIs("col") || token.NameIs("command") ||
                    token.NameIs("embed") || token.NameIs("hr") || token.NameIs("img") || token.NameIs("input") || token.NameIs("link") ||
                    token.NameIs("meta") || token.NameIs("param") || token.NameIs("source");
            }
        }

        private static void BuildHeadingNodes(
            MarkdownObject node,
            MarkdownEngine markdownEngine,
            List<ContentNode> documentNodes,
            Dictionary<FilePath, List<ContentNode>> inclusionDocumentNodes,
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
                if (node.IsInclude())
                {
                    var file = node.GetFilePath();
                    if (!inclusionDocumentNodes.TryGetValue(file, out var inclusionNodes))
                    {
                        inclusionDocumentNodes[file] = inclusionNodes = new List<ContentNode>();
                    }

                    inclusionNodes.Add(documentNode);
                }
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

        private static int GetFencedCodeBlockNetLineCount(FencedCodeBlock fencedCodeBlock)
        {
            int netLineCount = 0;

            for (int i = 0; i < fencedCodeBlock.Lines.Count; i++)
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
                SourceInfo = markdownNode.GetSourceInfo(),
            };
        }

        private static string GetHeadingContent(HeadingBlock headingBlock)
        {
            if (headingBlock.Inline is null || !headingBlock.Inline.Any())
            {
                return string.Empty;
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
