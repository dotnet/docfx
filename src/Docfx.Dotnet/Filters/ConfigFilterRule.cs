// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Docfx.Common;
using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal class ConfigFilterRule
{
    [YamlMember(Alias = "apiRules")]
    public List<ConfigFilterRuleItemUnion> ApiRules { get; set; } = [];

    [YamlMember(Alias = "attributeRules")]
    public List<ConfigFilterRuleItemUnion> AttributeRules { get; set; } = [];

    public bool CanVisitApi(SymbolFilterData symbol)
    {
        return CanVisitCore(ApiRules, symbol);
    }

    public bool CanVisitAttribute(SymbolFilterData symbol)
    {
        return CanVisitCore(AttributeRules, symbol);
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
        return true;
    }

    public static ConfigFilterRule Load(string configFile)
    {
        if (string.IsNullOrEmpty(configFile))
        {
            return new ConfigFilterRule();
        }
        if (!File.Exists(configFile)) throw new FileNotFoundException($"Filter Config file {configFile} does not exist!");

        ConfigFilterRule rule;
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
        var defaultConfigPath = $"{assembly.GetName().Name}.Resources.defaultfilterconfig.yml";
        using (var stream = assembly.GetManifestResourceStream(defaultConfigPath))
        {
            using var reader = new StreamReader(stream);
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
