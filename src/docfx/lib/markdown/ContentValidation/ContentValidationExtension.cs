// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        public static MarkdownPipelineBuilder UseContentValidation(this MarkdownPipelineBuilder builder, MarkdownContext markdownContext, Config config)
        {
            var validatorProvider = new OnlineServiceMarkdownValidatorProvider(
                new ContentValidationContext(),
                new ContentValidationLogger(markdownContext));

            return builder.Use(document =>
            {
                foreach (var validator in validatorProvider.GetValidators())
                {
                    validator.Validate(document);
                }
            });
        }
    }
}
