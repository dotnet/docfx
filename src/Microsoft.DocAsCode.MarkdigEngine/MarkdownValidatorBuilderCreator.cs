// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownValidatorBuilderCreator
    {
        public const string DefaultValidatorName = "default";
        public List<IMarkdownObjectValidatorProvider> ValidatorProviders { get; private set; } = new List<IMarkdownObjectValidatorProvider>();

        private readonly List<RuleWithId<MarkdownValidationRule>> _validators =
            new List<RuleWithId<MarkdownValidationRule>>();
        private readonly List<RuleWithId<MarkdownTagValidationRule>> _tagValidators =
            new List<RuleWithId<MarkdownTagValidationRule>>();
        private readonly Dictionary<string, MarkdownValidationRule> _globalValidators =
            new Dictionary<string, MarkdownValidationRule>();
        private readonly List<MarkdownValidationSetting> _settings =
            new List<MarkdownValidationSetting>();
        private ICompositionContainer Container;

        public MarkdownValidatorBuilderCreator(MarkdownServiceParameters parameters, ICompositionContainer container = null)
        {
            Container = container;
            LoadValidators(parameters);
        }

        public MarkdownValidatorBuilder CreateMarkdownValidatorBuilder()
        {
            return new MarkdownValidatorBuilder(ValidatorProviders, GetEnabledTagRules());
        }

        public void LoadValidators(MarkdownServiceParameters parameters)
        {
            if (parameters != null)
            {
                LoadValidatorConfig(parameters.BasePath, parameters.TemplateDir);
            }

            if (Container != null)
            {
                LoadEnabledRulesProvider();
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

        public IEnumerable<MarkdownTagValidationRule> GetEnabledTagRules()
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
            ValidatorProviders = (from name in enabledContractName
                                  from vp in Container?.GetExports<IMarkdownObjectValidatorProvider>(name)
                                  select vp).ToList();
        }

        private void LoadValidatorConfig(string baseDir, string templateDir)
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
                    LoadValidatorDefinition(configFolder);
                }
            }

            var configFile = Path.Combine(baseDir, MarkdownSytleConfig.MarkdownStyleFileName);
            if (EnvironmentContext.FileAbstractLayer.Exists(configFile))
            {
                var config = JsonUtility.Deserialize<MarkdownSytleConfig>(configFile);
                AddValidators(config.Rules);
                AddTagValidators(config.TagRules);
                AddSettings(config.Settings);
            }
            EnsureDefaultValidator();
        }

        private void LoadValidatorDefinition(string mdStyleDefPath)
        {
            if (Directory.Exists(mdStyleDefPath))
            {
                foreach (var configFile in Directory.GetFiles(mdStyleDefPath, "*" + MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix))
                {
                    var fileName = Path.GetFileName(configFile);
                    var category = fileName.Remove(fileName.Length - MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix.Length);
                    var config = JsonUtility.Deserialize<MarkdownSytleDefinition>(configFile);
                    AddTagValidators(category, config.TagRules);
                    AddValidators(category, config.Rules);
                }
            }
        }

        private void AddSettings(MarkdownValidationSetting[] settings)
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

        private sealed class RuleWithId<T>
        {
            public T Rule { get; set; }
            public string Category { get; set; }
            public string Id { get; set; }
        }
    }
}
