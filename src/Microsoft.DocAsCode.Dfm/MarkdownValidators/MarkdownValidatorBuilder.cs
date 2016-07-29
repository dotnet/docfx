// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.MarkdownValidators
{
    using System;
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
        public const string DefaultValidatorName = "default";
        public const string MarkdownValidatePhaseName = "Markdown style";

        private static readonly Regex OpeningTag = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
        private static readonly Regex ClosingTag = new Regex(@"^\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);

        public CompositionHost CompositionHost { get; }

        private readonly List<TagRuleWithId> _tagValidators = new List<TagRuleWithId>();

        private readonly Dictionary<string, MarkdownValidationRule> _validators =
            new Dictionary<string, MarkdownValidationRule>();

        public MarkdownValidatorBuilder(CompositionHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            CompositionHost = host;
        }

        public void AddTagValidators(string category, Dictionary<string, MarkdownTagValidationRule> validators)
        {
            if (validators == null)
            {
                return;
            }
            foreach (var pair in validators)
            {
                var fullId = category + ":" + pair.Key;
                _tagValidators.Add(new TagRuleWithId
                {
                    Category = category,
                    FullId = fullId,
                    TagRule = pair.Value,
                });
            }
        }

        public void AddTagValidators(MarkdownTagValidationRule[] validators)
        {
            if (validators == null)
            {
                return;
            }
            foreach (var item in validators)
            {
                _tagValidators.Add(new TagRuleWithId
                {
                    Category = null,
                    FullId = null,
                    TagRule = item,
                });
            }
        }

        public void AddValidators(MarkdownValidationRule[] rules)
        {
            if (rules == null)
            {
                return;
            }
            foreach (var rule in rules)
            {
                _validators[rule.RuleName] = rule;
            }
        }

        public void EnsureDefaultValidator()
        {
            if (!_validators.ContainsKey(DefaultValidatorName))
            {
                _validators[DefaultValidatorName] = new MarkdownValidationRule
                {
                    RuleName = DefaultValidatorName,
                };
            }
        }

        public IMarkdownTokenRewriter Create()
        {
            var list = new List<IMarkdownTokenValidator>();
            foreach (var contract in _validators)
            {
                if (!contract.Value.Disable)
                {
                    foreach (IMarkdownTokenValidatorProvider vp in CompositionHost.GetExports(typeof(IMarkdownTokenValidatorProvider), contract.Value.RuleName))
                    {
                        list.AddRange(vp.GetValidators());
                    }
                }
            }
            var context = new MarkdownRewriterContext(CompositionHost, GetEnabledTagRules().ToImmutableList());
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

        private IEnumerable<MarkdownTagValidationRule> GetEnabledTagRules()
        {
            foreach (var item in _tagValidators)
            {
                if (item.FullId != null)
                {
                    MarkdownValidationRule rule;
                    if (_validators.TryGetValue(item.FullId, out rule) ||
                        _validators.TryGetValue(item.Category, out rule))
                    {
                        if (!rule.Disable)
                        {
                            yield return item.TagRule;
                        }
                        continue;
                    }
                }
                if (!item.TagRule.Disable)
                {
                    yield return item.TagRule;
                }
            }
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
                        var hasTagName = validator.TagNames.Any(tagName => string.Equals(tagName, m.Groups[1].Value, System.StringComparison.OrdinalIgnoreCase));
                        if (hasTagName ^ (validator.Verb == TagVerb.NotIn))
                        {
                            ValidateOne(token, m, validator);
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
                        Logger.LogWarning(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.SourceInfo.Markdown), line: token.SourceInfo.LineNumber.ToString());
                        return;
                    case TagValidationBehavior.Error:
                        Logger.LogError(string.Format(validator.MessageFormatter, m.Groups[1].Value, token.SourceInfo.Markdown), line: token.SourceInfo.LineNumber.ToString());
                        return;
                    case TagValidationBehavior.None:
                    default:
                        return;
                }
            }
        }

        private sealed class TagRuleWithId
        {
            public MarkdownTagValidationRule TagRule { get; set; }
            public string Category { get; set; }
            public string FullId { get; set; }
        }
    }
}
