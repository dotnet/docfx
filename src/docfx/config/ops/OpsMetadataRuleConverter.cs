// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Docs.MetadataService.Models;
using Microsoft.Docs.Validation;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal static class OpsMetadataRuleConverter
{
    private static readonly Dictionary<string, string[]> s_ruleNameConvert = new()
    {
        { "Kind", new string[] { "unexpected-type" } },
        { "Match", new string[] { "invalid-value" } },
        { "Required", new string[] { "missing-attribute" } },
        { "Requires", new string[] { "missing-paired-attribute" } },
        { "List", new string[] { "invalid-paired-attribute", "invalid-value" } },
        { "Either", new string[] { "missing-either-attribute" } },
        { "Precludes", new string[] { "precluded-attributes" } },
        { "Date", new string[] { "date-format-invalid", "date-out-of-range" } },
        { "MicrosoftAlias", new string[] { "ms-alias-invalid" } },
        { "Deprecated", new string[] { "attribute-deprecated" } },
        { "Uniqueness", new string[] { "duplicate-attribute" } },
        { "Length", new string[] { "string-length-invalid" } },
    };

    public static string GenerateJsonSchema(string rulesContent, string allowlistsContent, ErrorBuilder errors)
    {
        try
        {
            return GenerateJsonSchemaCore(rulesContent, allowlistsContent);
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            errors.Add(Errors.System.ValidationIncomplete());
        }
        return "";
    }

    private static string GenerateJsonSchemaCore(string rulesContent, string allowlistsContent)
    {
        Log.Write(rulesContent);
        Log.Write(allowlistsContent);

        var rules = JsonConvert.DeserializeObject<Rules>(rulesContent);
        if (rules == null || rules.Count == 0)
        {
            return "";
        }

        var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(allowlistsContent) ?? new();

        var schema = new
        {
            docsetUnique = new List<string>(),
            properties = new Dictionary<string, dynamic>(),
            strictRequired = new List<string>(),
            dependencies = new Dictionary<string, List<string>>(),
            either = new List<List<string>>(),
            precludes = new List<List<string>>(),
            enumDependencies = new EnumDependenciesSchema(),
            rules = new Dictionary<string, Dictionary<string, dynamic>>(),
        };

        foreach (var (attribute, attributeRules) in rules)
        {
            // Only validate conceptual files for now
            var rulesInfo = new Dictionary<string, OpsMetadataRule>(
                from rule in attributeRules.Rules
                let ruleInfo = (OpsMetadataRule)rule.ToObject<OpsMetadataRule>()
                let type = ruleInfo.Type
                where type != null && !ruleInfo.Disabled
                select new KeyValuePair<string, OpsMetadataRule>(type, ruleInfo));

            var property = new Dictionary<string, object?>()
                {
                    { "type", GetType(rulesInfo) },
                    { "enum", GetEnum(rulesInfo) },
                    { "dateFormat", GetDateFormat(rulesInfo) },
                    { "relativeMaxDate", GetRelativeMaxDate(rulesInfo) },
                    { "relativeMinDate", GetRelativeMinDate(rulesInfo) },
                    { "replacedBy", GetReplacedBy(rulesInfo) },
                    { "microsoftAlias", GetMicrosoftAlias(rulesInfo, taxonomies) },
                    { "minLength", GetMinLength(rulesInfo) },
                    { "maxLength", GetMaxLength(rulesInfo) },
                };

            if (TryGetAttributeCustomRules(rulesInfo, out var attributeCustomRules))
            {
                schema.rules.Add(attribute, attributeCustomRules);
            }

            if (rulesInfo.ContainsKey("Uniqueness"))
            {
                schema.docsetUnique.Add(attribute);
            }

            if (rulesInfo.ContainsKey("Required"))
            {
                schema.strictRequired.Add(attribute);
            }

            if (rulesInfo.TryGetValue("Requires", out var requiresRuleInfo) && !string.IsNullOrEmpty(requiresRuleInfo.Name))
            {
                schema.dependencies.Add(attribute, new List<string>() { requiresRuleInfo.Name });
            }

            if (rulesInfo.TryGetValue("Precludes", out var precludesRuleInfo) && !string.IsNullOrEmpty(precludesRuleInfo.Name))
            {
                schema.precludes.Add(new List<string>() { attribute, precludesRuleInfo.Name });
            }

            if (rulesInfo.TryGetValue("Either", out var eitherRuleInfo) && !string.IsNullOrEmpty(eitherRuleInfo.Name))
            {
                schema.either.Add(new List<string>() { attribute, eitherRuleInfo.Name });
            }

            if (rulesInfo.TryGetValue("List", out var listRuleInfo) &&
                listRuleInfo != null &&
                TryGetTaxonomy(attribute, listRuleInfo.List, taxonomies, out var enumDependencies, out var enumValues))
            {
                // if just plain enum, extend property
                if (enumValues != null && enumValues.Length > 0)
                {
                    SetEnumValues(property, rulesInfo, enumValues);
                }
                else
                {
                    schema.enumDependencies.Add($"{attribute}[0]", enumDependencies);

                    if (rulesInfo.TryGetValue("Match", out var matchRuleInfo) &&
                        !string.IsNullOrEmpty(matchRuleInfo.Value) &&
                        !schema.enumDependencies[$"{attribute}[0]"].ContainsKey(matchRuleInfo.Value))
                    {
                        schema.enumDependencies[$"{attribute}[0]"].Add(matchRuleInfo.Value, null);
                    }
                }
            }

            var cleanProperty = property.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value);
            var propertyJson =
                JsonConvert.SerializeObject(
                    cleanProperty, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            if (propertyJson != "{}")
            {
                schema.properties.Add(attribute, cleanProperty);
            }
        }

        var jsonSchema = JsonConvert.SerializeObject(schema, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        Log.Write(jsonSchema);
        return jsonSchema;
    }

    private static void SetEnumValues(Dictionary<string, object?> property, Dictionary<string, OpsMetadataRule> rulesInfo, string[] enumValues)
    {
        var type = GetType(rulesInfo);
        if (type != null && type.Contains("array"))
        {
            var enumType = new
            {
                type = new string[] { "string", "null" },
                @enum = enumValues,
            };
            property.Add("items", enumType);
        }
        else
        {
            property["enum"] = enumValues;
        }
    }

    private static bool TryGetAttributeCustomRules(Dictionary<string, OpsMetadataRule> rulesInfo, out Dictionary<string, dynamic> attributeCustomRules)
    {
        attributeCustomRules = new Dictionary<string, dynamic>();

        foreach (var (ruleName, ruleInfo) in rulesInfo)
        {
            if (s_ruleNameConvert.TryGetValue(ruleName, out var baseCodes))
            {
                foreach (var baseCode in baseCodes)
                {
                    if (!attributeCustomRules.ContainsKey(baseCode))
                    {
                        attributeCustomRules.Add(baseCode, new
                        {
                            severity = string.IsNullOrEmpty(ruleInfo.Severity) ? null : ruleInfo.Severity.ToLowerInvariant(),
                            code = ruleInfo.Code,
                            additionalMessage = ruleInfo.AdditionalErrorMessage,
                            canonicalVersionOnly = ruleInfo.CanonicalVersionOnly,
                            pullRequestOnly = ruleInfo.PullRequestOnly,
                            contentTypes = ruleInfo.ContentTypes,
                            tags = ruleInfo.Tags,
                        });
                    }
                }
            }
        }

        return attributeCustomRules.Count != 0;
    }

    private static bool TryGetTaxonomy(
        string attribute,
        string? listId,
        Taxonomies taxonomies,
        out Dictionary<string, EnumDependenciesSchema?> taxonomy,
        out string[]? enumValues)
    {
        if (string.IsNullOrEmpty(listId) || !taxonomies.TryGetValue(listId["list:".Length..], out var subTaxonomy))
        {
            taxonomy = new Dictionary<string, EnumDependenciesSchema?>();
            enumValues = null;
            return false;
        }

        if (string.IsNullOrEmpty(subTaxonomy.NestedValue))
        {
            taxonomy = new Dictionary<string, EnumDependenciesSchema?>();
            enumValues = subTaxonomy.NestedTaxonomy.list;
            return true;
        }

        var nestedValue = subTaxonomy.NestedValue;

        // `slug` of product taxonomy means only one level indeed. Here combine parent and children
        if (string.Equals("slug", nestedValue, StringComparison.OrdinalIgnoreCase))
        {
            var list = new List<string>();
            foreach (var (key, value) in subTaxonomy.NestedTaxonomy.dic)
            {
                list.Add(key);
                list.AddRange(value);
            }
            enumValues = list.ToArray();
            taxonomy = new Dictionary<string, EnumDependenciesSchema?>();
            return true;
        }

        // msService => ms.service, msSubService => ms.subservice
        var strBuilder = new StringBuilder();
        var strIdx = 0;
        for (; strIdx < nestedValue.Length; strIdx++)
        {
            if (char.IsUpper(nestedValue[strIdx]))
            {
                strBuilder.Append('.');
                break;
            }
            strBuilder.Append(nestedValue[strIdx]);
        }
        nestedValue = strBuilder.Append(nestedValue[strIdx..]).ToString().ToLowerInvariant();

        taxonomy = new Dictionary<string, EnumDependenciesSchema?>();
        foreach (var (key, value) in subTaxonomy.NestedTaxonomy.dic)
        {
            // replace "(empty)" by ""
            var clearTaxonomy = value.ToDictionary(
                x => string.Equals("(empty)", x, StringComparison.OrdinalIgnoreCase) ? string.Empty : x, x => (EnumDependenciesSchema?)null);

            taxonomy.Add(key, new EnumDependenciesSchema() { { $"{nestedValue}[0]", clearTaxonomy } });
        }
        enumValues = null;
        return true;
    }

    private static object? GetMicrosoftAlias(Dictionary<string, OpsMetadataRule> rulesInfo, Taxonomies taxonomies)
    {
        if (rulesInfo.TryGetValue("MicrosoftAlias", out var microsoftAliasRuleInfo) &&
            !string.IsNullOrEmpty(microsoftAliasRuleInfo.AllowedDLs) &&
            taxonomies.TryGetValue(microsoftAliasRuleInfo.AllowedDLs["list:".Length..], out var taxonomy) &&
            string.IsNullOrEmpty(taxonomy.NestedValue))
        {
            return new { allowedDLs = taxonomy.NestedTaxonomy.list };
        }

        return null;
    }

    private static string? GetReplacedBy(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Deprecated", out var deprecatedRuleInfo))
        {
            return deprecatedRuleInfo.ReplacedBy;
        }

        return null;
    }

    private static TimeSpan? GetRelativeMaxDate(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Date", out var dateRuleInfo))
        {
            return dateRuleInfo.RelativeMax;
        }

        return null;
    }

    private static TimeSpan? GetRelativeMinDate(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Date", out var dateRuleInfo))
        {
            return dateRuleInfo.RelativeMin;
        }

        return null;
    }

    private static string? GetDateFormat(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Date", out var dateRuleInfo))
        {
            return dateRuleInfo.Format;
        }

        return null;
    }

    private static int? GetMinLength(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Length", out var lengthRuleInfo))
        {
            return lengthRuleInfo.MinLength;
        }

        return null;
    }

    private static int? GetMaxLength(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Length", out var lengthRuleInfo))
        {
            return lengthRuleInfo.MaxLength;
        }

        return null;
    }

    private static List<string>? GetType(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Kind", out var kindRuleInfo) && kindRuleInfo.MultipleValues.HasValue)
        {
            return kindRuleInfo.MultipleValues.Value ? new List<string> { "array", "null" } : new List<string> { "string", "null" };
        }

        return null;
    }

    private static List<string>? GetEnum(Dictionary<string, OpsMetadataRule> rulesInfo)
    {
        if (rulesInfo.TryGetValue("Kind", out var kindRuleInfo) && kindRuleInfo.MultipleValues.HasValue && kindRuleInfo.MultipleValues.Value)
        {
            return null;
        }

        if (rulesInfo.TryGetValue("Match", out var matchRuleInfo) && !string.IsNullOrEmpty(matchRuleInfo.Value) && !rulesInfo.ContainsKey("List"))
        {
            return new List<string>() { matchRuleInfo.Value };
        }

        return null;
    }

    private class EnumDependenciesSchema : Dictionary<string, Dictionary<string, EnumDependenciesSchema?>> { }
}
