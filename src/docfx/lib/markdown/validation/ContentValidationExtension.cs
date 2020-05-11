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
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        public static MarkdownPipelineBuilder UseContentValidation(
            this MarkdownPipelineBuilder builder,
            OnlineServiceMarkdownValidatorProvider? validatorProvider,
            Func<List<ValidationNode>, Dictionary<Document, (List<ValidationNode> nodes, bool isIncluded)>> getValidationNodes,
            Func<string, object, MarkdownObject, (string? content, object? file)> readFile)
        {
            var validators = validatorProvider?.GetValidators();
            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    var documentNodes = new List<ValidationNode>();
                    document.Visit(node =>
                    {
                        if (node is HeadingBlock headingBlock)
                        {
                            documentNodes.Add(new Heading
                            {
                                Level = headingBlock.Level,
                                SourceInfo = headingBlock.ToSourceInfo(),
                                Content = GetHeadingContent(headingBlock), // used for reporting
                                HeadingChar = headingBlock.HeaderChar,
                                RenderedPlainText = MarkdigUtility.ToPlainText(headingBlock), // used for validation
                                IsVisible = MarkdigUtility.IsVisible(headingBlock),
                            });
                        }
                        else if (node is InclusionBlock inclusionBlock)
                        {
                            // Heading block cannot be in the InclusionInline
                            var inclusionDocument = (Document?)readFile(inclusionBlock.IncludedFilePath, InclusionContext.File, node).file;
                            if (inclusionDocument != null)
                            {
                                documentNodes.Add(new InclusionNode
                                {
                                    SourceInfo = node.ToSourceInfo(),
                                    IncludedFilePath = inclusionDocument.FilePath.ToString(),
                                });
                            }
                        }
                        else if (node is LeafBlock leafBlock)
                        {
                            documentNodes.Add(new ContentNode
                            {
                                SourceInfo = node.ToSourceInfo(),
                                IsVisible = MarkdigUtility.IsVisible(leafBlock),
                            });
                        }

                        return false;
                    });

                    var allNodes = getValidationNodes(documentNodes);

                    if (InclusionContext.IsInclude)
                    {
                        foreach (var (doc, (nodes, _)) in allNodes)
                        {
                            var currentFile = (Document)InclusionContext.File;
                            if (doc == currentFile)
                            {
                                continue;
                            }

                            var index = 0;
                            while (index < nodes.Count)
                            {
                                var current = nodes[index];
                                if (current is InclusionNode inclusionNode && inclusionNode.IncludedFilePath == $"{currentFile.FilePath}")
                                {
                                    nodes.RemoveAt(index);
                                    nodes.InsertRange(index, documentNodes.Select(node =>
                                    {
                                        var newNode = (ValidationNode)node.Clone();
                                        newNode.InclusionSourceInfo = node.SourceInfo;
                                        newNode.SourceInfo = current.SourceInfo;
                                        return newNode;
                                    }));
                                    index += documentNodes.Count - 1;
                                }

                                index++;
                            }
                        }
                    }

                    if (validators != null)
                    {
                        foreach (var validator in validators)
                        {
                            validator.Validate(document);
                        }
                    }
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

        private class InclusionNode : ValidationNode, ICloneable
        {
            public string? IncludedFilePath { get; set; }

            public InclusionNode()
            {
            }

            protected InclusionNode(InclusionNode inclusionNode)
                : base(inclusionNode)
            {
                this.IncludedFilePath = inclusionNode.IncludedFilePath;
            }

            public override object Clone()
            {
                return new InclusionNode(this);
            }
        }
    }
}
