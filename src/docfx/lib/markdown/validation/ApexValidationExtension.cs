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
        // An internal HACK to workaround the `SourceInfo.ToString` behavior dependency.
        // TODO: remove this after we retire apex validation.
        [ThreadStatic]
        private static bool s_forceSourceInfoToStringFilePathOnly;

        public static bool ForceSourceInfoToStringFilePathOnly => s_forceSourceInfoToStringFilePathOnly;

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
                var filePath = ((SourceInfo)InclusionContext.File).File;
                if (filePath.Format == FileFormat.Markdown)
                {
                    var layout = getLayout(filePath);
                    if (layout != "HubPage" && layout != "LandingPage")
                    {
                        try
                        {
                            s_forceSourceInfoToStringFilePathOnly = true;

                            foreach (var validator in validators)
                            {
                                validator.Validate(document);
                            }
                        }
                        finally
                        {
                            s_forceSourceInfoToStringFilePathOnly = false;
                        }
                    }
                }
            });
        }
    }
}
