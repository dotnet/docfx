// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.MarkdownValidators
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownValidatorBuilder
    {
        #region Consts
        public const string DefaultValidatorName = "default";
        public const string MarkdownValidatePhaseName = "Markdown style";
        #endregion

        #region Fields
        private readonly List<RuleWithId<MarkdownMetadataValidationRule>> _metadataValidators =
            new List<RuleWithId<MarkdownMetadataValidationRule>>();
        private readonly List<RuleWithId<MarkdownTagValidationRule>> _tagValidators =
            new List<RuleWithId<MarkdownTagValidationRule>>();
        private readonly List<RuleWithId<MarkdownValidationRule>> _validators =
            new List<RuleWithId<MarkdownValidationRule>>();
        private readonly List<MarkdownValidationSetting> _settings =
            new List<MarkdownValidationSetting>();
        private readonly Dictionary<string, MarkdownMetadataValidationRule> _globalMetadataValidators =
            new Dictionary<string, MarkdownMetadataValidationRule>();
        private readonly Dictionary<string, MarkdownValidationRule> _globalValidators =
            new Dictionary<string, MarkdownValidationRule>();
        #endregion

        #region Ctors

        public MarkdownValidatorBuilder(ICompositionContainer container)
        {
            Container = container;
        }

        #endregion

        #region Properties
        public ICompositionContainer Container { get; }
        #endregion

        #region Public Methods

        public static MarkdownValidatorBuilder Create(ICompositionContainer container, string baseDir, string templateDir)
        {
            var builder = new MarkdownValidatorBuilder(container);
            LoadValidatorConfig(baseDir, templateDir, builder);
            return builder;
        }

        public void AddMetadataValidators(string category, Dictionary<string, MarkdownMetadataValidationRule> validators)
        {
            if (validators == null)
            {
                return;
            }
            foreach (var pair in validators)
            {
                if (string.IsNullOrEmpty(pair.Value.ContractName))
                {
                    continue;
                }
                _metadataValidators.Add(new RuleWithId<MarkdownMetadataValidationRule>
                {
                    Category = category,
                    Id = pair.Key,
                    Rule = pair.Value,
                });
            }
        }

        public void AddMetadataValidators(MarkdownMetadataValidationRule[] rules)
        {
            if (rules == null)
            {
                return;
            }
            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.ContractName))
                {
                    continue;
                }
                _globalMetadataValidators[rule.ContractName] = rule;
            }
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
                if (string.IsNullOrEmpty(pair.Value.ContractName))
                {
                    continue;
                }
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
                if (string.IsNullOrEmpty(rule.ContractName))
                {
                    continue;
                }
                _globalValidators[rule.ContractName] = rule;
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
                    ContractName = DefaultValidatorName,
                };
            }
        }

        public IMarkdownTokenRewriter CreateRewriter()
        {
            var context = new MarkdownRewriterContext(Container, GetEnabledTagRules().ToImmutableList());
            return new MarkdownTokenRewriteWithScope(
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownValidatePhaseName,
                    GetEnabledRules().Concat(
                        new[]
                        {
                            MarkdownTokenValidatorFactory.FromLambda<IMarkdownToken>(context.Validate)
                        })),
                MarkdownValidatePhaseName);
        }

        public IEnumerable<IInputMetadataValidator> GetEnabledMetadataRules()
        {
            HashSet<string> enabledContractName = new HashSet<string>();
            foreach (var item in _metadataValidators)
            {
                if (IsDisabledBySetting(item) ?? item.Rule.Disable)
                {
                    enabledContractName.Remove(item.Rule.ContractName);
                }
                else
                {
                    enabledContractName.Add(item.Rule.ContractName);
                }
            }
            foreach (var pair in _globalMetadataValidators)
            {
                if (pair.Value.Disable)
                {
                    enabledContractName.Remove(pair.Value.ContractName);
                }
                else
                {
                    enabledContractName.Add(pair.Value.ContractName);
                }
            }
            return from name in enabledContractName
                   from IInputMetadataValidator mv in Container?.GetExports<IInputMetadataValidator>(name)
                   select mv;
        }

        #endregion

        #region Private Methods

        private static void LoadValidatorConfig(string baseDir, string templateDir, MarkdownValidatorBuilder builder)
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                return;
            }
            if (templateDir != null)
            {
                var configFolder = Path.Combine(templateDir, MarkdownSytleDefinition.MarkdownStyleDefinitionFolderName);
                if (Directory.Exists(configFolder))
                {
                    LoadValidatorDefinition(configFolder, builder);
                }
            }
            var configFile = Path.Combine(baseDir, MarkdownSytleConfig.MarkdownStyleFileName);
            if (EnvironmentContext.FileAbstractLayer.Exists(configFile))
            {
                var config = JsonUtility.Deserialize<MarkdownSytleConfig>(configFile);
                builder.AddMetadataValidators(config.MetadataRules);
                builder.AddValidators(config.Rules);
                builder.AddTagValidators(config.TagRules);
                builder.AddSettings(config.Settings);
            }
            builder.EnsureDefaultValidator();
        }

        private static void LoadValidatorDefinition(string mdStyleDefPath, MarkdownValidatorBuilder builder)
        {
            if (Directory.Exists(mdStyleDefPath))
            {
                foreach (var configFile in Directory.GetFiles(mdStyleDefPath, "*" + MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix))
                {
                    var fileName = Path.GetFileName(configFile);
                    var category = fileName.Remove(fileName.Length - MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix.Length);
                    var config = JsonUtility.Deserialize<MarkdownSytleDefinition>(configFile);
                    builder.AddMetadataValidators(category, config.MetadataRules);
                    builder.AddTagValidators(category, config.TagRules);
                    builder.AddValidators(category, config.Rules);
                }
            }
        }

        private IEnumerable<IMarkdownTokenValidator> GetEnabledRules()
        {
            HashSet<string> enabledContractName = new HashSet<string>();
            foreach (var item in _validators)
            {
                if (IsDisabledBySetting(item) ?? item.Rule.Disable)
                {
                    enabledContractName.Remove(item.Rule.ContractName);
                }
                else
                {
                    enabledContractName.Add(item.Rule.ContractName);
                }
            }
            foreach (var pair in _globalValidators)
            {
                if (pair.Value.Disable)
                {
                    enabledContractName.Remove(pair.Value.ContractName);
                }
                else
                {
                    enabledContractName.Add(pair.Value.ContractName);
                }
            }
            if (Container == null)
            {
                return Enumerable.Empty<IMarkdownTokenValidator>();
            }
            return from name in enabledContractName
                   from vp in Container.GetExports<IMarkdownTokenValidatorProvider>(name)
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

        #endregion

        #region Nested Classes

        private sealed class MarkdownRewriterContext
        {
            private static readonly Regex OpeningTag = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
            private static readonly Regex ClosingTag = new Regex(@"^\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>$", RegexOptions.Compiled);
            private static readonly Regex OpeningTagMatcher = new Regex(@"^\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>", RegexOptions.Compiled);

            public MarkdownRewriterContext(ICompositionContainer container, ImmutableList<MarkdownTagValidationRule> validators)
            {
                Container = container;
                Validators = validators;
            }

            public ICompositionContainer Container { get; }

            public ImmutableList<MarkdownTagValidationRule> Validators { get; }

            public void Validate(IMarkdownToken token)
            {
                var text = GetTokenText(token);
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                var m = OpeningTag.Match(text);
                bool isOpeningTag = true;
                if (m.Length == 0)
                {
                    m = ClosingTag.Match(text);
                    if (m.Length == 0)
                    {
                        return;
                    }
                    isOpeningTag = false;
                }

                ValidateCore(token, m, isOpeningTag);
            }

            private string GetTokenText(IMarkdownToken token)
            {
                if (token is MarkdownTagInlineToken)
                {
                    return token.SourceInfo.Markdown;
                }
                if (token is MarkdownRawToken &&
                    (token.Rule is MarkdownHtmlBlockRule || token.Rule is MarkdownPreElementInlineRule))
                {
                    return OpeningTagMatcher.Match(token.SourceInfo.Markdown).Value;
                }
                return null;
            }

            private void ValidateCore(IMarkdownToken token, Match m, bool isOpeningTag)
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

            private void ValidateOne(IMarkdownToken token, Match m, MarkdownTagValidationRule validator)
            {
                if (!string.IsNullOrEmpty(validator.CustomValidatorContractName))
                {
                    if (Container == null)
                    {
                        Logger.LogWarning($"Unable to validate tag by contract({validator.CustomValidatorContractName}): CompositionHost is null.");
                        return;
                    }
                    var customValidators = GetCustomMarkdownTagValidators(validator);
                    if (customValidators != null && customValidators.Count == 0)
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
                return Container
                    ?.GetExports<ICustomMarkdownTagValidator>(validator.CustomValidatorContractName)
                    .Cast<ICustomMarkdownTagValidator>()
                    .ToList();
            }

            private void ValidateOneCore(IMarkdownToken token, Match m, MarkdownTagValidationRule validator)
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

        #endregion
    }
}
