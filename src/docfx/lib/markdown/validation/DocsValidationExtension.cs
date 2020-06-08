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
                var file = (Document)InclusionContext.File;
                if (file.FilePath.Format != FileFormat.Markdown)
                {
                    return;
                }

                var documentNodes = new List<ContentNode>();
                var inclusionDocumentNodes = new Dictionary<Document, List<ContentNode>>();
                MarkdigUtility.Visit(document, null, null, (node, parents, current) =>
                {
                    if (node is HeadingBlock headingBlock)
                    {
                        var heading = new Heading
                        {
                            Level = headingBlock.Level,
                            SourceInfo = headingBlock.GetSourceInfo(),
                            ParentSourceInfoList = parents?.Cast<object?>().ToList() ?? new List<object?>(),
                            Content = GetHeadingContent(headingBlock), // used for reporting
                            HeadingChar = headingBlock.HeaderChar,
                            RenderedPlainText = markdownEngine.ToPlainText(headingBlock), // used for validation
                            IsVisible = MarkdigUtility.IsVisible(headingBlock),
                        };

                        if (current != null && parents != null)
                        {
                            if (!inclusionDocumentNodes.TryGetValue(current, out var contentNodes))
                            {
                                inclusionDocumentNodes[current] = contentNodes = new List<ContentNode>();
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

                contentValidator.ValidateHeadings(file, documentNodes, false);
                foreach (var (key, nodes) in inclusionDocumentNodes)
                {
                    contentValidator.ValidateHeadings(key, nodes, true);
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
