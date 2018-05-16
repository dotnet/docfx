// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Markdig.Syntax;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownValidatorBuilder
    {
        private List<IMarkdownObjectValidatorProvider> _validatorProviders;
        private IEnumerable<MarkdownTagValidationRule> _enabledTagRules;

        public MarkdownValidatorBuilder(List<IMarkdownObjectValidatorProvider> validatorProviders, IEnumerable<MarkdownTagValidationRule> enabledTagRules)
        {
            _validatorProviders = validatorProviders;
            _enabledTagRules = enabledTagRules;
        }

        public IMarkdownObjectRewriter CreateRewriter(MarkdownContext context)
        {
            var tagValidator = new TagValidator(_enabledTagRules.ToImmutableList(), context);
            var validators = from vp in _validatorProviders
                             from p in vp.GetValidators()
                             select p;

            return MarkdownObjectRewriterFactory.FromValidators(
                    validators.Concat(
                        new[]
                        {
                            MarkdownObjectValidatorFactory.FromLambda<IMarkdownObject>(tagValidator.Validate)
                        }));
        }
    }
}
