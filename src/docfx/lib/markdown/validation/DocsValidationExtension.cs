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
                var currentFile = (Document)InclusionContext.File;
                if (currentFile.FilePath.Format != FileFormat.Markdown)
                {
                    return;
                }

                var documentNodes = new List<ContentNode>();
                var inclusionDocumentNodes = new Dictionary<Document, List<ContentNode>>();
                var codeBlockItemList = new List<(bool IsIncluded, CodeBlockItem codeBlockItem)>();

                var canonicalVersion = getCanonicalVersion();
                var fileLevelMoniker = getFileLevelMonikers();

                MarkdigUtility.Visit(document, new MarkdownVisitContext(currentFile), (node, context) =>
                {
                    var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, node.GetZoneLevelMonikers());

                    BuildHeadingNodes(node, context, markdownEngine, documentNodes, inclusionDocumentNodes, isCanonicalVersion);

                    BuildCodeBlockNodes(node, context, codeBlockItemList, isCanonicalVersion);

                    return false;
                });

                contentValidator.ValidateHeadings(currentFile, documentNodes, false);
                foreach (var (inclusion, inclusionNodes) in inclusionDocumentNodes)
                {
                    contentValidator.ValidateHeadings(inclusion, inclusionNodes, true);
                }

                foreach (var codeBlock in codeBlockItemList)
                {
                    contentValidator.ValidateCodeBlock(currentFile, codeBlock.codeBlockItem, codeBlock.IsIncluded);
                }
            });
        }

        private static void BuildHeadingNodes(
            MarkdownObject node,
            MarkdownVisitContext context,
            MarkdownEngine markdownEngine,
            List<ContentNode> documentNodes,
            Dictionary<Document, List<ContentNode>> inclusionDocumentNodes,
            bool? isCanonicalVersion)
        {
            ContentNode? documentNode = null;

            switch (node)
            {
                case HeadingBlock headingBlock:
                    var headingNode = CreateValidationNode<Heading>(context, isCanonicalVersion, headingBlock);

                    headingNode.Level = headingBlock.Level;
                    headingNode.Content = GetHeadingContent(headingBlock); // used for reporting
                    headingNode.HeadingChar = headingBlock.HeaderChar;
                    headingNode.RenderedPlainText = markdownEngine.ToPlainText(headingBlock); // used for validation
                    headingNode.IsVisible = MarkdigUtility.IsVisible(headingBlock);

                    documentNode = headingNode;
                    break;

                case LeafBlock leafBlock:
                    var contentNode = CreateValidationNode<ContentNode>(context, isCanonicalVersion, leafBlock);
                    contentNode.IsVisible = MarkdigUtility.IsVisible(leafBlock);
                    documentNode = contentNode;
                    break;

                default:
                    break;
            }

            if (documentNode != null)
            {
                documentNodes.Add(documentNode);
                if (context.IsInclude)
                {
                    if (!inclusionDocumentNodes.TryGetValue(context.Document, out var inclusionNodes))
                    {
                        inclusionDocumentNodes[context.Document] = inclusionNodes = new List<ContentNode>();
                    }

                    inclusionNodes.Add(documentNode);
                }
            }
        }

        private static void BuildCodeBlockNodes(
            MarkdownObject node,
            MarkdownVisitContext context,
            List<(bool IsInclude, CodeBlockItem codeBlockItem)> codeBlockItemList,
            bool? isCanonicalVersion)
        {
            CodeBlockItem? codeBlockItem = null;

            switch (node)
            {
                case FencedCodeBlock fencedCodeBlock:
                    codeBlockItem = CreateValidationNode<CodeBlockItem>(context, isCanonicalVersion, node);

                    codeBlockItem.Type = CodeBlockTypeEnum.FencedCodeBlock;
                    codeBlockItem.Info = fencedCodeBlock.Info;
                    codeBlockItem.Arguments = fencedCodeBlock.Arguments;
                    codeBlockItem.IsOpen = fencedCodeBlock.IsOpen;
                    break;

                case YamlFrontMatterBlock _:
                    break;

                case CodeBlock codeBlock:
                    codeBlockItem = CreateValidationNode<CodeBlockItem>(context, isCanonicalVersion, codeBlock);
                    codeBlockItem.Type = CodeBlockTypeEnum.CodeBlock;
                    break;

                default:
                    break;
            }

            if (codeBlockItem != null)
            {
                codeBlockItemList.Add((context.IsInclude, codeBlockItem));
            }
        }

        private static bool? IsCanonicalVersion(string? canonicalVersion, MonikerList fileLevelMonikerList, MonikerList zoneLevelMonikerList)
        {
            if (zoneLevelMonikerList.HasMonikers)
            {
                return zoneLevelMonikerList.IsCanonicalVersion(canonicalVersion);
            }

            return fileLevelMonikerList.IsCanonicalVersion(canonicalVersion);
        }

        private static T CreateValidationNode<T>(MarkdownVisitContext context, bool? isCanonicalVersion, MarkdownObject markdownNode)
            where T : ValidationNode, new()
        {
            return new T()
            {
                IsCanonicalVersion = isCanonicalVersion,
                ParentSourceInfoList = context.Parents?.Cast<object?>().ToList() ?? new List<object?>(),
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
