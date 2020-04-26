// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Syntax;
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
            if (validatorProvider == null)
            {
                return builder;
            }

            var validators = validatorProvider.GetValidators();
            var headings = new List<Heading>();
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (node is HeadingBlock headingBlock)
                    {
                        headings.Add(new Heading
                        {
                            Level = headingBlock.Level,
                            SourceInfo = headingBlock.ToSourceInfo(),
                            Content = GetHeadingContent(headingBlock),
                            // HeadingChar = headingBlock.HeadingChar
                        });
                    }

                    return false;
                });

                contentValidator.ValidateHeadings((Document)InclusionContext.File, headings);

                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    foreach (var validator in validators)
                    {
                        validator.Validate(document);
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

            return "TODO";
        }
    }
}
