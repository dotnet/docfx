// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Docs.MetadataService.Models;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class OpsMetadataRuleConverter
    {
        private static readonly Dictionary<string, string[]> s_ruleNameConvert = new Dictionary<string, string[]>()
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

        public static string GenerateJsonSchema(string rulesContent, string allowlistsContent)
        {
            Log.Write(rulesContent);
            Log.Write(allowlistsContent);

            var rules = JsonConvert.DeserializeObject<Rules>(rulesContent);
            if (rules == null || rules.Count == 0)
            {
                return "";
            }

            var allowlists = JsonConvert.DeserializeObject<Allowlists>(allowlistsContent);

            var schema = new
            {
                docsetUnique = new List<string>(),
                properties = new Dictionary<string, dynamic>(),
                strictRequired = new List<string>(),
                dependencies = new Dictionary<string, List<string>>(),
                either = new List<List<string>>(),
                precludes = new List<List<string>>(),
                enumDependencies = new EnumDependenciesSchema(),
                customRules = new Dictionary<string, Dictionary<string, dynamic>>(),
            };

            foreach (var (attribute, attributeRules) in rules)
            {
                // Only validate conceptual files for now
                var rulesInfo = new Dictionary<string, OpsMetadataRule>(
                    from rule in attributeRules.Rules
                    let ruleInfo = (OpsMetadataRule)rule.ToObject<OpsMetadataRule>()
                    let type = ruleInfo.Type
                    where ruleInfo.ContentTypes.Contains("conceptual") && type != null && !ruleInfo.Disabled
                    select new KeyValuePair<string, OpsMetadataRule>(type, ruleInfo));

                var property = new
                {
                    type = GetType(rulesInfo),
                    @enum = GetEnum(rulesInfo),
                    dateFormat = GetDateFormat(rulesInfo),
                    relativeMaxDate = GetRelativeMaxDate(rulesInfo),
                    relativeMinDate = GetRelativeMinDate(rulesInfo),
                    replacedBy = GetReplacedBy(rulesInfo),
                    microsoftAlias = GetMicrosoftAlias(rulesInfo, allowlists),
                    minLength = GetMinLength(rulesInfo),
                    maxLength = GetMaxLength(rulesInfo),
                };

                var propertyJson =
                    JsonConvert.SerializeObject(property, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                if (!string.Equals("{}", propertyJson))
                {
                    schema.properties.Add(attribute, property);
                }

                if (TryGetAttributeCustomRules(rulesInfo, out var attributeCustomRules))
                {
                    schema.customRules.Add(attribute, attributeCustomRules);
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
                    TryGetAllowlist(attribute, listRuleInfo.List, allowlists, 0, out var allowlist))
                {
                    schema.enumDependencies.Add($"{attribute}[0]", allowlist);

                    if (rulesInfo.TryGetValue("Match", out var matchRuleInfo) &&
                        !string.IsNullOrEmpty(matchRuleInfo.Value) &&
                        !schema.enumDependencies[$"{attribute}[0]"].ContainsKey(matchRuleInfo.Value))
                    {
                        schema.enumDependencies[$"{attribute}[0]"].Add(matchRuleInfo.Value, null);
                    }
                }
            }

            var jsonSchema = JsonConvert.SerializeObject(schema, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Log.Write(jsonSchema);
            return jsonSchema;
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
                            });
                        }
                    }
                }
            }

            return attributeCustomRules.Count != 0;
        }

        private static bool TryGetAllowlist(
            string parentName, string? listId, Allowlists allowlists, int index, out Dictionary<string, EnumDependenciesSchema?> allowList)
        {
            allowList = new Dictionary<string, EnumDependenciesSchema?>();

            if (!string.IsNullOrEmpty(listId) && allowlists.TryGetValue(listId, out var mainAllowlist))
            {
                if (string.IsNullOrEmpty(mainAllowlist.DependentAttribute))
                {
                    allowList = mainAllowlist.Values.Keys.ToDictionary(validValue => validValue, validValue => (EnumDependenciesSchema?)null);
                    return true;
                }

                if (string.Equals(parentName, mainAllowlist.DependentAttribute))
                {
                    index++;
                }

                foreach (var (validValue, subListId) in mainAllowlist.Values)
                {
                    if (TryGetAllowlist(mainAllowlist.DependentAttribute, subListId, allowlists, index, out var subAllowList))
                    {
                        allowList.Add(validValue, new EnumDependenciesSchema() { { $"{mainAllowlist.DependentAttribute}[{index}]", subAllowList } });
                    }
                }

                return true;
            }

            return false;
        }

        private static object? GetMicrosoftAlias(Dictionary<string, OpsMetadataRule> rulesInfo, Allowlists allowlists)
        {
            if (rulesInfo.TryGetValue("MicrosoftAlias", out var microsoftAliasRuleInfo) &&
                !string.IsNullOrEmpty(microsoftAliasRuleInfo.AllowedDLs) &&
                allowlists.TryGetValue(microsoftAliasRuleInfo.AllowedDLs, out var allowlist))
            {
                return new { allowedDLs = allowlist.Values.Keys.ToList() };
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
}
