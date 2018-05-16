// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownValidatorBuilder
    {
        private readonly List<RuleWithId<MarkdownValidationRule>> _validators =
            new List<RuleWithId<MarkdownValidationRule>>();
        private readonly List<RuleWithId<MarkdownTagValidationRule>> _tagValidators =
            new List<RuleWithId<MarkdownTagValidationRule>>();
        private readonly Dictionary<string, MarkdownValidationRule> _globalValidators =
            new Dictionary<string, MarkdownValidationRule>();
        private readonly List<MarkdownValidationSetting> _settings =
            new List<MarkdownValidationSetting>();
        private List<IMarkdownObjectValidatorProvider> _validatorProviders =
            new List<IMarkdownObjectValidatorProvider>();

        public const string DefaultValidatorName = "default";
        public const string MarkdownValidatePhaseName = "Markdown style";

        private ICompositionContainer Container { get; }

        public MarkdownValidatorBuilder(ICompositionContainer container)
        {
            Container = container;
        }

        public static MarkdownValidatorBuilder Create(
            MarkdownServiceParameters parameters,
            ICompositionContainer container)
        {
            var builder = new MarkdownValidatorBuilder(container);
            if (parameters != null)
            {
                LoadValidatorConfig(parameters.BasePath, parameters.TemplateDir, builder);
            }

            if (container != null)
            {
                builder.LoadEnabledRulesProvider();
            }

            return builder;
        }

        public IMarkdownObjectRewriter CreateRewriter(MarkdownContext context)
        {
            var tagValidator = new TagValidator(GetEnabledTagRules().ToImmutableList(), context);
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
                    Rule = item
                });
            }
        }

        internal void AddTagValidators(string category, Dictionary<string, MarkdownTagValidationRule> validators)
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

        internal void AddSettings(MarkdownValidationSetting[] settings)
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

        private void EnsureDefaultValidator()
        {
            if (!_globalValidators.ContainsKey(DefaultValidatorName))
            {
                _globalValidators[DefaultValidatorName] = new MarkdownValidationRule
                {
                    ContractName = DefaultValidatorName
                };
            }
        }

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
                    builder.AddTagValidators(category, config.TagRules);
                    builder.AddValidators(category, config.Rules);
                }
            }
        }

        public void LoadEnabledRulesProvider()
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
            _validatorProviders = (from name in enabledContractName
                                   from vp in Container?.GetExports<IMarkdownObjectValidatorProvider>(name)
                                   select vp).ToList();
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

        #region Nested Classes
        private sealed class RuleWithId<T>
        {
            public T Rule { get; set; }
            public string Category { get; set; }
            public string Id { get; set; }
        }
        #endregion
    }
}
