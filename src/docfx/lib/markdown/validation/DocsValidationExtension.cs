// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Docs.Validation;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal static class DocsValidationExtension
    {
        public static MarkdownPipelineBuilder UseDocsValidation(
            this MarkdownPipelineBuilder builder,
            MarkdownEngine markdownEngine,
            ContentValidator contentValidator)
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
                MarkdigUtility.Visit(document, new MarkdownVisitContext(currentFile), (node, context) =>
                {
                    if (node is HeadingBlock headingBlock)
                    {
                        var heading = new Heading
                        {
                            Level = headingBlock.Level,
                            SourceInfo = headingBlock.GetSourceInfo(),
                            ParentSourceInfoList = context.Parents?.Cast<object?>().ToList() ?? new List<object?>(),
                            Content = GetHeadingContent(headingBlock), // used for reporting
                            HeadingChar = headingBlock.HeaderChar,
                            RenderedPlainText = markdownEngine.ToPlainText(headingBlock), // used for validation
                            IsVisible = MarkdigUtility.IsVisible(headingBlock),
                        };

                        if (context.IsInclude)
                        {
                            if (!inclusionDocumentNodes.TryGetValue(context.Document, out var contentNodes))
                            {
                                inclusionDocumentNodes[context.Document] = contentNodes = new List<ContentNode>();
                            }

                            contentNodes.Add(heading);
                        }

                        documentNodes.Add(heading);
                    }
                    else if (node is LeafBlock leafBlock)
                    {
                        documentNodes.Add(new ContentNode
                        {
                            SourceInfo = node.GetSourceInfo(),
                            IsVisible = MarkdigUtility.IsVisible(leafBlock),
                        });
                    }

                    return false;
                });

                contentValidator.ValidateHeadings(currentFile, documentNodes, false);
                foreach (var (inclusion, inclusionNodes) in inclusionDocumentNodes)
                {
                    contentValidator.ValidateHeadings(inclusion, inclusionNodes, true);
                }
            });
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
