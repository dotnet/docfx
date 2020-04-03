// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        public static MarkdownPipelineBuilder UseContentValidation(
            this MarkdownPipelineBuilder builder, MarkdownContext markdownContext, string markdownValidationRules)
        {
            if (string.IsNullOrEmpty(markdownValidationRules))
            {
                return builder;
            }

            var validatorProvider = new OnlineServiceMarkdownValidatorProvider(
                new ContentValidationContext(markdownValidationRules),
                new ContentValidationLogger(markdownContext));

            var validators = validatorProvider.GetValidators();

            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    foreach (var validator in validators)
                    {
                        validator.Validate(document);
                    }
                }
            });
        }
    }
}
