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
        public IEnumerable<ConfigFilterRuleItemUnion> ApiRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        [YamlMember(Alias = "attributeRules")]
        public IEnumerable<ConfigFilterRuleItemUnion> AttributeRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        public bool CanVisitApi(SymbolFilterData symbol)
        {
            return CanVisitCore(this.ApiRules, symbol);
        }

        public bool CanVisitAttribute(SymbolFilterData symbol)
        {
            return CanVisitCore(this.AttributeRules, symbol);
        }

        public static ConfigFilterRule Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                return new ConfigFilterRule();
            }

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException($"Filter Config file {configFile} does not exist!");
            }

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
            ConfigFilterRule defaultRule;

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

            var userRule = Load(filterConfigFile);
            return Merge(defaultRule, userRule);
        }

        private static bool CanVisitCore(IEnumerable<ConfigFilterRuleItemUnion> ruleItems, SymbolFilterData symbol)
        {
            foreach (var ruleUnion in ruleItems)
            {
                ConfigFilterRuleItem rule = ruleUnion.Rule;
                if (rule != null && rule.IsMatch(symbol))
                {
                    return rule.CanVisit;
                }
            }

            return TryVisitSymbolWithPriorSpecializedRuling(ruleItems.Where(item => item.Rule != null), symbol);
        }

        private static ConfigFilterRule Merge(ConfigFilterRule defaultRule, ConfigFilterRule userRule)
        {
            return new ConfigFilterRule
            {
                // user rule always overwrite default rule
                ApiRules = userRule.ApiRules.Concat(defaultRule.ApiRules),
                AttributeRules = userRule.AttributeRules.Concat(defaultRule.AttributeRules),
            };
        }

        private static bool TryVisitSymbolWithPriorSpecializedRuling(IEnumerable<ConfigFilterRuleItemUnion> apiRules, SymbolFilterData filterData)
        {
            if (!apiRules.Any())
            {
                return false;
            }

            string GetCleanUidString(string inputString)
            {
                if (string.IsNullOrEmpty(inputString))
                {
                    return string.Empty;
                }

                if (!inputString.Contains("^") && !inputString.Contains(@"\"))
                {
                    // NOT a UID Regex. 
                    return inputString;
                }

                return inputString.Replace("^", string.Empty).Replace(@"\", string.Empty);
            }

            bool DoesApiRuleIdMatchesSymbol(ConfigFilterRuleItem apiRule)
            {
                var apiIdentification = GetCleanUidString(apiRule?.UidRegex);
                return apiIdentification != null && !string.IsNullOrWhiteSpace(filterData.Id) && apiIdentification.Contains(filterData.Id);
            }

            bool IsMatchedRulingConflictingWithFilters(ConfigFilterRuleItem apiRule)
            {
                ////var ruleSet = apiRules.Except(apiRules);
                return false;
            }

            return apiRules.FirstOrDefault(apiRule => DoesApiRuleIdMatchesSymbol(apiRule.Rule) && !IsMatchedRulingConflictingWithFilters(apiRule.Rule)) != null;
        }
    }
}
