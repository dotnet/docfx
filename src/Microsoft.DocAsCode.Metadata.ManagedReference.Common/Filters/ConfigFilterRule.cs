// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;

    [Serializable]
    public class ConfigFilterRule
    {
        [YamlMember(Alias = "apiRules")]
        public List<ConfigFilterRuleItemUnion> ApiRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        [YamlMember(Alias = "attributeRules")]
        public List<ConfigFilterRuleItemUnion> AttributeRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        public bool CanVisitApi(SymbolFilterData symbol)
        {
            return this.CanVisitCore(this.ApiRules, symbol);
        }

        public bool CanVisitAttribute(SymbolFilterData symbol)
        {
            return this.CanVisitCore(this.AttributeRules, symbol);
        }

        private bool CanVisitCore(IEnumerable<ConfigFilterRuleItemUnion> ruleItems, SymbolFilterData symbol)
        {
            foreach (var ruleUnion in ruleItems)
            {
                ConfigFilterRuleItem rule = ruleUnion.Rule;
                if (rule != null && rule.IsMatch(symbol))
                {
                    return rule.CanVisit;
                }
            }
            return true;
        }

        public static ConfigFilterRule Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                return new ConfigFilterRule();
            }
            if (!File.Exists(configFile)) throw new FileNotFoundException($"Filter Config file {configFile} does not exist!");

            ConfigFilterRule rule = null;
            try
            {
                rule = YamlUtility.Deserialize<ConfigFilterRule>(configFile);
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"Error parsing filter config file {configFile}: {e.Message}");
            }

            if (rule == null)
            {
                throw new InvalidDataException($"Unable to deserialize filter config {configFile}.");
            }
            return rule;
        }

        public static ConfigFilterRule LoadWithDefaults(string filterConfigFile)
        {
            ConfigFilterRule defaultRule, userRule;

            var assembly = Assembly.GetExecutingAssembly();
            var defaultConfigPath = $"{assembly.GetName().Name}.Filters.defaultfilterconfig.yml";
            using (var stream = assembly.GetManifestResourceStream(defaultConfigPath))
            using (var reader = new StreamReader(stream))
            {
                defaultRule = YamlUtility.Deserialize<ConfigFilterRule>(reader);
            }

            if (string.IsNullOrEmpty(filterConfigFile))
            {
                return defaultRule;
            }
            else
            {
                userRule = Load(filterConfigFile);
                return Merge(defaultRule, userRule);
            }
        }

        private static ConfigFilterRule Merge(ConfigFilterRule defaultRule, ConfigFilterRule userRule)
        {
            return new ConfigFilterRule
            {
                // user rule always overwrite default rule
                ApiRules = userRule.ApiRules.Concat(defaultRule.ApiRules).ToList(),
                AttributeRules = userRule.AttributeRules.Concat(defaultRule.AttributeRules).ToList(),
            };
        }
    }
}
