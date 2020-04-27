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
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        public static MarkdownPipelineBuilder UseContentValidation(
            this MarkdownPipelineBuilder builder, OnlineServiceMarkdownValidatorProvider? validatorProvider, ContentValidator contentValidator)
        {
            if (validatorProvider == null && contentValidator == null)
            {
                return builder;
            }

            var validators = validatorProvider?.GetValidators();
            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    if (contentValidator != null)
                    {
                        var headings = new List<Heading>();
                        document.Visit(node =>
                        {
                            if (node is HeadingBlock headingBlock)
                            {
                                headings.Add(new Heading
                                {
                                    Level = headingBlock.Level,
                                    SourceInfo = new SourceInfo(((Document)InclusionContext.File).FilePath, 0, 0),
                                    Content = GetHeadingContent(headingBlock),

                                    // HeadingChar = headingBlock.HeadingChar
                                });
                            }

                            return false;
                        });

                        contentValidator.ValidateHeadings((Document)InclusionContext.File, headings);
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
