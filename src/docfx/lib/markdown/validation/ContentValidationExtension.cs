// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.DocAsCode.MarkdigEngine.Validators;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        private static ImmutableArray<IMarkdownObjectValidator> _validators;
        private static object locker = new object();

        public static MarkdownPipelineBuilder UseContentValidation(this MarkdownPipelineBuilder builder, MarkdownContext markdownContext, Func<string> getMarkdownValidationRulesPath)
        {
            return builder.Use(document =>
            {
                if (string.IsNullOrEmpty(getMarkdownValidationRulesPath()))
                {
                    return;
                }

                if (_validators.IsDefaultOrEmpty)
                {
                    lock (locker)
                    {
                        if (_validators.IsDefaultOrEmpty)
                        {
                            var validatorProvider = new OnlineServiceMarkdownValidatorProvider(
                                new ContentValidationContext(getMarkdownValidationRulesPath()),
                                new ContentValidationLogger(markdownContext));
                            _validators = validatorProvider.GetValidators();
                        }
                    }
                }

                foreach (var validator in _validators)
                {
                    validator.Validate(document);
                }
            });
        }
    }
}
