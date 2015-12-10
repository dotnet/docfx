// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownValidators
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition.Hosting;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownRewriterBuilder
    {
        private static readonly Regex OpeningTag = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
        private static readonly Regex ClosingTag = new Regex(@"^\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);

        public CompositionHost CompositionHost { get; }

        public ImmutableList<MarkdownTagValidationRule> Validators { get; set; }

        public MarkdownRewriterBuilder(CompositionHost host)
        {
            CompositionHost = host;
            Validators = ImmutableList<MarkdownTagValidationRule>.Empty;
        }

        public void AddValidators(params MarkdownTagValidationRule[] validators)
        {
            Validators = Validators.AddRange(validators);
        }

        public IMarkdownRewriter Create()
        {
            var context = new MarkdownRewriterContext(CompositionHost, Validators);
            return MarkdownRewriterFactory.FromLambda<MarkdownEngine, MarkdownTagInlineToken>(context.Validate);
        }

        private sealed class MarkdownRewriterContext
        {
            public MarkdownRewriterContext(CompositionHost host, ImmutableList<MarkdownTagValidationRule> validators)
            {
                CompositionHost = host;
                Validators = validators;
            }

            public CompositionHost CompositionHost { get; }

            public ImmutableList<MarkdownTagValidationRule> Validators { get; }

            public IMarkdownToken Validate(MarkdownEngine engine, MarkdownTagInlineToken token)
            {
                var m = OpeningTag.Match(token.Content);
                bool isOpeningTag = true;
                if (m.Length == 0)
                {
                    m = ClosingTag.Match(token.Content);
                    if (m.Length == 0)
                    {
                        return null;
                    }
                    isOpeningTag = false;
                }

                return ValidateCore(token, m, isOpeningTag);
            }

            private IMarkdownToken ValidateCore(MarkdownTagInlineToken token, Match m, bool isOpeningTag)
            {
                foreach (var validator in Validators)
                {
                    if (isOpeningTag || !validator.OpeningTagOnly)
                    {
                        foreach (var tagName in validator.TagNames)
                        {
                            if (string.Equals(tagName, m.Groups[1].Value, System.StringComparison.OrdinalIgnoreCase))
                            {
                                return ValidateOne(token, m, validator);
                            }
                        }
                    }
                }
                return null;
            }

            private IMarkdownToken ValidateOne(MarkdownTagInlineToken token, Match m, MarkdownTagValidationRule validator)
            {
                if (!string.IsNullOrEmpty(validator.CustomValidatorContractName))
                {
                    if (CompositionHost == null)
                    {
                        Logger.LogWarning($"Unable to validate tag by contract({validator.CustomValidatorContractName}): CompositionHost is null.");
                        return null;
                    }
                    var customValidators = GetCustomMarkdownTagValidators(validator);
                    if (customValidators.Count == 0)
                    {
                        Logger.LogWarning($"Cannot find custom markdown tag validator by contract name: {validator.CustomValidatorContractName}.");
                        return null;
                    }
                    if (customValidators.TrueForAll(av => av.Validate(token.Content)))
                    {
                        return null;
                    }
                }
                return ValidateOneCore(token, m, validator);
            }

            private List<ICustomMarkdownTagValidator> GetCustomMarkdownTagValidators(MarkdownTagValidationRule validator)
            {
                return CompositionHost
                    .GetExports(typeof(ICustomMarkdownTagValidator), validator.CustomValidatorContractName)
                    .Cast<ICustomMarkdownTagValidator>()
                    .ToList();
            }

            private IMarkdownToken ValidateOneCore(MarkdownTagInlineToken token, Match m, MarkdownTagValidationRule validator)
            {
                switch (validator.Behavior)
                {
                    case TagRewriteBehavior.Warning:
                        Logger.LogWarning(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.Content));
                        return null;
                    case TagRewriteBehavior.Error:
                        Logger.LogError(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.Content));
                        return null;
                    case TagRewriteBehavior.ErrorAndRemove:
                        Logger.LogError(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.Content));
                        return new MarkdownIgnoreToken(token.Rule);
                    case TagRewriteBehavior.None:
                    default:
                        return null;
                }
            }
        }
    }
}
