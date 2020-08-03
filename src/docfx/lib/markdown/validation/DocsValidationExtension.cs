// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
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
                var codeBlockItemList = new List<CodeBlockItem>();

                var canonicalVersion = getCanonicalVersion();
                var fileLevelMoniker = getFileLevelMonikers();
                MarkdigUtility.Visit(document, new MarkdownVisitContext(currentFile), (node, context) =>
                {
                    HeadingValidation(node, context, markdownEngine, documentNodes, inclusionDocumentNodes, canonicalVersion, fileLevelMoniker);

                    CodeBlockValidation(node, context, codeBlockItemList, canonicalVersion, fileLevelMoniker);

                    return false;
                });

                contentValidator.ValidateHeadings(currentFile, documentNodes, false);
                foreach (var (inclusion, inclusionNodes) in inclusionDocumentNodes)
                {
                    contentValidator.ValidateHeadings(inclusion, inclusionNodes, true);
                }

                foreach (var codeBlockItem in codeBlockItemList)
                {
                    contentValidator.ValidateCodeBlock(currentFile, codeBlockItem);
                }
            });
        }

        private static void HeadingValidation(
            MarkdownObject node,
            MarkdownVisitContext context,
            MarkdownEngine markdownEngine,
            List<ContentNode> documentNodes,
            Dictionary<Document, List<ContentNode>> inclusionDocumentNodes,
            string? canonicalVersion,
            MonikerList fileLevelMoniker)
        {
            var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, context.ZoneMoniker);

            ContentNode? documentNode = null;
            if (node is HeadingBlock headingBlock)
            {
                documentNode = new Heading
                {
                    Level = headingBlock.Level,
                    SourceInfo = headingBlock.GetSourceInfo(),
                    ParentSourceInfoList = context.Parents?.Cast<object?>().ToList() ?? new List<object?>(),
                    Content = GetHeadingContent(headingBlock), // used for reporting
                    HeadingChar = headingBlock.HeaderChar,
                    RenderedPlainText = markdownEngine.ToPlainText(headingBlock), // used for validation
                    IsVisible = MarkdigUtility.IsVisible(headingBlock),
                    IsCanonicalVersion = isCanonicalVersion,
                    Zone = context.ZoneStack.TryPeek(out var z) ? z : null,
                    Monikers = context.ZoneMoniker.ToList(),
                };
            }
            else if (node is LeafBlock leafBlock)
            {
                documentNode = new ContentNode
                {
                    SourceInfo = node.GetSourceInfo(),
                    IsVisible = MarkdigUtility.IsVisible(leafBlock),
                    IsCanonicalVersion = isCanonicalVersion,
                    Zone = context.ZoneStack.TryPeek(out var z) ? z : null,
                    Monikers = context.ZoneMoniker.ToList(),
                };
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

        private static void CodeBlockValidation(
            MarkdownObject node,
            MarkdownVisitContext context,
            List<CodeBlockItem> codeBlockItemList,
            string? canonicalVersion,
            MonikerList fileLevelMoniker)
        {
            var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, context.ZoneMoniker);

            CodeBlockItem? codeBlockItem = null;

            if (node is FencedCodeBlock fencedCodeBlock)
            {
                codeBlockItem = new CodeBlockItem
                {
                    Type = CodeBlockTypeEnum.FencedCodeBlock,
                    Info = fencedCodeBlock.Info,
                    Arguments = fencedCodeBlock.Arguments,
                    IsOpen = fencedCodeBlock.IsOpen,
                    SourceInfo = fencedCodeBlock.GetSourceInfo(),
                    IsCanonicalVersion = isCanonicalVersion,
                };
            }
            else if (node is CodeBlock codeBlock && context.TripleColonStack.Count == 0)
            {
                codeBlockItem = new CodeBlockItem
                {
                    Type = CodeBlockTypeEnum.CodeBlock,
                    SourceInfo = codeBlock.GetSourceInfo(),
                    IsCanonicalVersion = isCanonicalVersion,
                };
            }

            if (codeBlockItem != null)
            {
                codeBlockItemList.Add(codeBlockItem);
            }
        }

        private static bool? IsCanonicalVersion(string? canonicalVersion, MonikerList fileLevelMonikerList, MonikerList zoneLevelMonikerList)
        {
            if (zoneLevelMonikerList.HasMonikers)
            {
                return MonikerList.IsCanonicalVersion(canonicalVersion, zoneLevelMonikerList);
            }

            return MonikerList.IsCanonicalVersion(canonicalVersion, fileLevelMonikerList);
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
