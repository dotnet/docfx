// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ApexValidationExtension
    {
        public static MarkdownPipelineBuilder UseApexValidation(
            this MarkdownPipelineBuilder builder,
            OnlineServiceMarkdownValidatorProvider? validatorProvider,
            Func<FilePath, string?> getLayout)
        {
            var validators = validatorProvider?.GetValidators();
            if (validators is null)
            {
                return builder;
            }

            return builder.Use(document =>
            {
                var filePath = ((Document)InclusionContext.File).FilePath;
                if (filePath.Format == FileFormat.Markdown)
                {
                    var layout = getLayout(filePath);
                    if (layout != "HubPage" && layout != "LandingPage")
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
