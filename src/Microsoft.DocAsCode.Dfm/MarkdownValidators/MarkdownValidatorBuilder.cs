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

        public CompositionHost CompositionHost { get; }

        private readonly List<RuleWithId<MarkdownTagValidationRule>> _tagValidators =
            new List<RuleWithId<MarkdownTagValidationRule>>();
        private readonly List<RuleWithId<MarkdownValidationRule>> _validators =
            new List<RuleWithId<MarkdownValidationRule>>();
        private readonly List<MarkdownValidationSetting> _settings =
            new List<MarkdownValidationSetting>();
        private readonly Dictionary<string, MarkdownValidationRule> _globalValidators =
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
                _tagValidators.Add(new RuleWithId<MarkdownTagValidationRule>
                {
                    Category = category,
                    Id = pair.Key,
                    Rule = pair.Value,
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
                _tagValidators.Add(new RuleWithId<MarkdownTagValidationRule>
                {
                    Category = null,
                    Id = null,
                    Rule = item,
                });
            }
        }

        public void AddValidators(string category, Dictionary<string, MarkdownValidationRule> validators)
        {
            if (validators == null)
            {
                return;
            }
            foreach (var pair in validators)
            {
                _validators.Add(new RuleWithId<MarkdownValidationRule>
                {
                    Category = category,
                    Id = pair.Key,
                    Rule = pair.Value,
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
                _globalValidators[rule.RuleName] = rule;
            }
        }

        public void AddSettings(MarkdownValidationSetting[] settings)
        {
            if (settings == null)
            {
                return;
            }
            foreach (var setting in settings)
            {
                _settings.Add(setting);
            }
        }

        public void EnsureDefaultValidator()
        {
            if (!_globalValidators.ContainsKey(DefaultValidatorName))
            {
                _globalValidators[DefaultValidatorName] = new MarkdownValidationRule
                {
                    RuleName = DefaultValidatorName,
                };
            }
        }

        public IMarkdownTokenRewriter Create()
        {
            var context = new MarkdownRewriterContext(CompositionHost, GetEnabledTagRules().ToImmutableList());
            return new MarkdownTokenRewriteWithScope(
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownValidatePhaseName,
                    GetEnabledRules().Concat(
                        new[]
                        {
                            MarkdownTokenValidatorFactory.FromLambda<MarkdownTagInlineToken>(context.Validate)
                        })),
                MarkdownValidatePhaseName);
        }

        private IEnumerable<IMarkdownTokenValidator> GetEnabledRules()
        {
            var list = new List<IMarkdownTokenValidator>();
            HashSet<string> enabledContractName = new HashSet<string>();
            foreach (var item in _validators)
            {
                if (IsDisabledBySetting(item) ?? item.Rule.Disable)
                {
                    enabledContractName.Remove(item.Rule.RuleName);
                }
                else
                {
                    enabledContractName.Add(item.Rule.RuleName);
                }
            }
            foreach (var pair in _globalValidators)
            {
                if (pair.Value.Disable)
                {
                    enabledContractName.Remove(pair.Value.RuleName);
                }
                else
                {
                    enabledContractName.Add(pair.Value.RuleName);
                }
            }
            return from name in enabledContractName
                   from IMarkdownTokenValidatorProvider vp in CompositionHost.GetExports(typeof(IMarkdownTokenValidatorProvider), name)
                   from v in vp.GetValidators()
                   select v;
        }

        private IEnumerable<MarkdownTagValidationRule> GetEnabledTagRules()
        {
            foreach (var item in _tagValidators)
            {
                if (IsDisabledBySetting(item) ?? item.Rule.Disable)
                {
                    continue;
                }
                yield return item.Rule;
            }
        }

        private bool? IsDisabledBySetting<T>(RuleWithId<T> item)
        {
            bool? categoryDisable = null;
            bool? idDisable = null;
            if (item.Category != null)
            {
                foreach (var setting in _settings)
                {
                    if (setting.Category == item.Category)
                    {
                        if (setting.Id == null)
                        {
                            categoryDisable = setting.Disable;
                        }
                        else if (setting.Id == item.Id)
                        {
                            idDisable = setting.Disable;
                        }
                    }
                }
            }
            return idDisable ?? categoryDisable;
        }

        private sealed class MarkdownRewriterContext
        {
            private static readonly Regex OpeningTag = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
            private static readonly Regex ClosingTag = new Regex(@"^\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);

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
                        if (hasTagName ^ (validator.Relation == TagRelation.NotIn))
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

        private sealed class RuleWithId<T>
        {
            public T Rule { get; set; }
            public string Category { get; set; }
            public string Id { get; set; }
        }


        private sealed class MarkdownTokenRewriteWithScope : IMarkdownTokenRewriter, IInitializable
        {
            public IMarkdownTokenRewriter Inner { get; }

            public string Scope { get; }

            public MarkdownTokenRewriteWithScope(IMarkdownTokenRewriter inner, string scope)
            {
                Inner = inner;
                Scope = scope;
            }

            public void Initialize(IMarkdownRewriteEngine rewriteEngine)
            {
                using (string.IsNullOrEmpty(Scope) ? null : new LoggerPhaseScope(Scope))
                {
                    (Inner as IInitializable)?.Initialize(rewriteEngine);
                }
            }

            public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
            {
                using (string.IsNullOrEmpty(Scope) ? null : new LoggerPhaseScope(Scope))
                {
                    return Inner.Rewrite(engine, token);
                }
            }
        }
    }
}
