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

                        if (node is InclusionBlock inclusionBlock)
                        {
                            var inclusionDocument = (Document?)readFile(inclusionBlock.IncludedFilePath, InclusionContext.File, inclusionBlock).file;
                            if (inclusionDocument != null)
                            {
                                documentHeadings.Add(new Heading
                                {
                                    Level = -1,
                                    SourceInfo = inclusionBlock.ToSourceInfo(),
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
                            var headingPlaceHolders = rootFileHeadings.Where(h => h.Level == -1 && h.Content == ((Document)InclusionContext.File).FilePath.ToString()).ToList();
                            foreach (var headingPlaceHolder in headingPlaceHolders)
                            {
                                rootFileHeadings.InsertRange(rootFileHeadings.IndexOf(headingPlaceHolder), documentHeadings);
                                rootFileHeadings.Remove(headingPlaceHolder);
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
