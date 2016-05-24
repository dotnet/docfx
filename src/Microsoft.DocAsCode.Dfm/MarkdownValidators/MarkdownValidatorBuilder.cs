// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.MarkdownValidators
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition.Hosting;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownValidatorBuilder
    {
        public const string MarkdownValidatePhaseName = "Markdown style";

        private static readonly Regex OpeningTag = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
        private static readonly Regex ClosingTag = new Regex(@"^\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);

        public CompositionHost CompositionHost { get; }

        public ImmutableList<MarkdownTagValidationRule> TagValidators { get; set; }

        public ImmutableList<string> ValidatorContracts { get; set; }

        public MarkdownValidatorBuilder(CompositionHost host)
        {
            CompositionHost = host;
            TagValidators = ImmutableList<MarkdownTagValidationRule>.Empty;
            ValidatorContracts = ImmutableList<string>.Empty;
        }

        public void AddTagValidators(params MarkdownTagValidationRule[] validators)
        {
            AddTagValidators((IEnumerable<MarkdownTagValidationRule>)validators);
        }

        public void AddTagValidators(IEnumerable<MarkdownTagValidationRule> validators)
        {
            TagValidators = TagValidators.AddRange(validators);
        }

        public void AddValidators(params string[] validatorContracts)
        {
            AddValidators((IEnumerable<string>)validatorContracts);
        }

        public void AddValidators(IEnumerable<string> validatorContracts)
        {
            ValidatorContracts = ValidatorContracts.AddRange(validatorContracts);
        }

        public IMarkdownTokenRewriter Create()
        {
            var list = new List<IMarkdownTokenValidator>();
            foreach (var contract in ValidatorContracts)
            {
                foreach (IMarkdownTokenValidatorProvider vp in CompositionHost.GetExports(typeof(IMarkdownTokenValidatorProvider), contract))
                {
                    list.AddRange(vp.GetValidators());
                }
            }
            var context = new MarkdownRewriterContext(CompositionHost, TagValidators);
            list.Add(MarkdownTokenValidatorFactory.FromLambda<MarkdownTagInlineToken>(context.Validate));
            return MarkdownTokenRewriterFactory.FromLambda(
                (IMarkdownRewriteEngine engine, IMarkdownToken token) =>
                {
                    using (new LoggerPhaseScope(MarkdownValidatePhaseName))
                    {
                        foreach (var item in list)
                        {
                            item.Validate(token);
                        }
                    }
                    return null;
                });
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

            public void Validate(MarkdownTagInlineToken token)
            {
                var m = OpeningTag.Match(token.SourceInfo.Markdown);
                bool isOpeningTag = true;
                if (m.Length == 0)
                {
                    m = ClosingTag.Match(token.SourceInfo.Markdown);
                    if (m.Length == 0)
                    {
                        return;
                    }
                    isOpeningTag = false;
                }

                ValidateCore(token, m, isOpeningTag);
            }

            private void ValidateCore(MarkdownTagInlineToken token, Match m, bool isOpeningTag)
            {
                foreach (var validator in Validators)
                {
                    if (isOpeningTag || !validator.OpeningTagOnly)
                    {
                        foreach (var tagName in validator.TagNames)
                        {
                            if (string.Equals(tagName, m.Groups[1].Value, System.StringComparison.OrdinalIgnoreCase))
                            {
                                ValidateOne(token, m, validator);
                                return;
                            }
                        }
                    }
                }
                return;
            }

            private void ValidateOne(MarkdownTagInlineToken token, Match m, MarkdownTagValidationRule validator)
            {
                if (!string.IsNullOrEmpty(validator.CustomValidatorContractName))
                {
                    if (CompositionHost == null)
                    {
                        Logger.LogWarning($"Unable to validate tag by contract({validator.CustomValidatorContractName}): CompositionHost is null.");
                        return;
                    }
                    var customValidators = GetCustomMarkdownTagValidators(validator);
                    if (customValidators.Count == 0)
                    {
                        Logger.LogWarning($"Cannot find custom markdown tag validator by contract name: {validator.CustomValidatorContractName}.");
                        return;
                    }
                    if (customValidators.TrueForAll(av => av.Validate(token.SourceInfo.Markdown)))
                    {
                        return;
                    }
                }
                ValidateOneCore(token, m, validator);
            }

            private List<ICustomMarkdownTagValidator> GetCustomMarkdownTagValidators(MarkdownTagValidationRule validator)
            {
                return CompositionHost
                    .GetExports(typeof(ICustomMarkdownTagValidator), validator.CustomValidatorContractName)
                    .Cast<ICustomMarkdownTagValidator>()
                    .ToList();
            }

            private void ValidateOneCore(MarkdownTagInlineToken token, Match m, MarkdownTagValidationRule validator)
            {
                switch (validator.Behavior)
                {
                    case TagValidationBehavior.Warning:
                        Logger.LogWarning(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.SourceInfo.Markdown));
                        return;
                    case TagValidationBehavior.Error:
                        Logger.LogError(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.SourceInfo.Markdown));
                        return;
                    case TagValidationBehavior.None:
                    default:
                        return;
                }
            }
        }
    }
}
