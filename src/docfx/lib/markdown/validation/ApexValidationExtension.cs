// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Validations.DocFx.Adapter;

#pragma warning disable CS0618

namespace Microsoft.Docs.Build
{
    internal static class ApexValidationExtension
    {
        public static MarkdownPipelineBuilder UseApexValidation(
            this MarkdownPipelineBuilder builder,
            OnlineServiceMarkdownValidatorProvider? validatorProvider)
        {
            var validators = validatorProvider?.GetValidators();
            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
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
    }
}
