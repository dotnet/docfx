// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;

    public class ConfigFilterVisitor : DelegatingFilterVisitor
    {
        private string _configFile;
        private ConfigFilterRule _configRule;

        public ConfigFilterVisitor(IFilterVisitor inner, string configFile)
            : base(inner)
        {
            _configFile = configFile;
            _configRule = LoadRules(configFile);
        }

        public override bool CanVisitApi(ISymbol symbol, bool wantProtectedMember = true)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if (!Inner.CanVisitApi(symbol, wantProtectedMember))
            {
                return false;
            }

            return CanVisitCore(_configRule.ApiRules, CanVisitApi, symbol, wantProtectedMember);
        }

        private bool CanVisitCore(IEnumerable<ConfigFilterRuleItemUnion> ruleItems, Func<ISymbol, bool, bool> visitFunc, ISymbol symbol, bool wantProtectedMember = true)
        {
            var current = symbol;
            var parent = symbol.ContainingSymbol;
            while (!(current is INamespaceSymbol) && parent != null)
            {
                if (!visitFunc(parent, wantProtectedMember))
                {
                    return false;
                }

                current = parent;
                parent = parent.ContainingSymbol;
            }

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

        private static ConfigFilterRule LoadRules(string configFile)
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
    }
}