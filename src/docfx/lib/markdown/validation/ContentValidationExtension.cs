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
            Func<List<Heading>, Dictionary<Document, (List<Heading> headings, bool isIncluded)>> getHeadings,
            Func<string, object, MarkdownObject, (string? content, object? file)> readFile)
        {
            var validators = validatorProvider?.GetValidators();
            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    var documentHeadings = new List<Heading>();
                    document.Visit(node =>
                    {
                        if (node is HeadingBlock headingBlock)
                        {
                            documentHeadings.Add(new Heading
                            {
                                Level = headingBlock.Level,
                                SourceInfo = headingBlock.ToSourceInfo(),
                                Content = GetHeadingContent(headingBlock), // used for reporting
                                HeadingChar = headingBlock.HeaderChar,
                                RenderedPlainText = MarkdigUtility.ToPlainText(headingBlock), // used for validation
                            });
                        }

                        if (node is InclusionBlock || node is InclusionInline)
                        {
                            var includedFilePath = node is InclusionInline inline ? inline.IncludedFilePath : ((InclusionBlock)node).IncludedFilePath;
                            var inclusionDocument = (Document?)readFile(includedFilePath, InclusionContext.File, node).file;
                            if (inclusionDocument != null)
                            {
                                documentHeadings.Add(new Heading
                                {
                                    Level = -1,
                                    SourceInfo = node.ToSourceInfo(),
                                    Content = inclusionDocument.FilePath.ToString(),
                                });
                            }
                        }

                        return false;
                    });

                    var allHeadings = getHeadings(documentHeadings);

                    if (InclusionContext.IsInclude && documentHeadings.Any())
                    {
                        var rootFile = (Document)InclusionContext.RootFile;
                        var rootFileHeadings = allHeadings.TryGetValue(rootFile, out var headings) ? headings.headings : null;
                        if (rootFileHeadings != null)
                        {
                            var index = 0;
                            while (index < rootFileHeadings.Count)
                            {
                                var current = rootFileHeadings[index];
                                if (current.Level == -1 && current.Content == $"{((Document)InclusionContext.File).FilePath}")
                                {
                                    rootFileHeadings.RemoveAt(index);
                                    rootFileHeadings.InsertRange(index, documentHeadings);
                                    index += documentHeadings.Count - 1;
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
    }
}
