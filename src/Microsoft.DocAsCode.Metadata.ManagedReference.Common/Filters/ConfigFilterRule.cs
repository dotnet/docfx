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

    /// <summary>
    ///     Defines the configuration filter rules to use in order to determine which APIs can be documented with the tool.
    /// </summary>
    [Serializable]
    public class ConfigFilterRule
    {
        /// <summary>
        ///     Validates whether the user provided a filter document in the configuration file (docfx.json)
        /// </summary>
        private static bool DidUserProvideFilterDocument;

        /// <summary>
        ///     Anchor used in a Regex pattern to indicate to match on any string that start with an indicated string.
        /// </summary>
        private static readonly string RegexStartOfStringAnchor = "^";

        /// <summary>
        ///     An escaped special character used in a path.
        /// </summary>
        private static readonly string EscapedSpecialCharacterInNamespacePath = @"\";

        /// <summary>
        ///     The API rules indicated by the user.
        /// </summary>
        [YamlMember(Alias = "apiRules")]
        public IEnumerable<ConfigFilterRuleItemUnion> ApiRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        /// <summary>
        ///     The API attributes rules indicated by the user.
        /// </summary>
        [YamlMember(Alias = "attributeRules")]
        public IEnumerable<ConfigFilterRuleItemUnion> AttributeRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        /// <summary>
        ///     Method responsible to validate if it's possible to visit an API.
        /// </summary>
        /// <param name="symbol">
        ///     The symbol to analyze.
        /// </param>
        /// <returns>
        ///     When the method returns <see langword="true"/>, DocFX is able to extract the metadata of the API.
        /// </returns>
        public bool CanVisitApi(SymbolFilterData symbol)
        {
            return CanVisitCore(this.ApiRules, symbol);
        }

        /// <summary>
        ///     Method responsible to validate if it's possible to visit the attributes on an API.
        /// </summary>
        /// <param name="symbol">
        ///     The symbol to analyze.
        /// </param>
        /// <returns>
        ///     When the method returns <see langword="true"/>, DocFX is able to extract the metadata of the API.
        /// </returns>
        public bool CanVisitAttribute(SymbolFilterData symbol)
        {
            return CanVisitCore(this.AttributeRules, symbol);
        }

        /// <summary>
        ///     Load a user defined filtering rule for APIs.
        /// </summary>
        /// <param name="filterDocumentFilePath">
        ///     The path to the filter document.
        /// </param>
        /// <returns>
        ///     A loaded filtering rule for APIs
        /// </returns>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="filterDocumentFilePath"/> the path to the file does not exist.
        /// </exception>
        /// <exception cref="InvalidDataException">
        ///     When there was an error while deserializing the filtering document.
        /// </exception>
        /// <exception cref="InvalidDataException">
        ///     When it's impossible to deserialize the filtering document.
        /// </exception>
        public static ConfigFilterRule LoadFilteringRule(string filterDocumentFilePath)
        {
            if (string.IsNullOrWhiteSpace(filterDocumentFilePath))
            {
                return new ConfigFilterRule();
            }

            if (!File.Exists(filterDocumentFilePath))
            {
                throw new FileNotFoundException($"Filter Config file {filterDocumentFilePath} does not exist!");
            }

            DidUserProvideFilterDocument = true;
            ConfigFilterRule rule;
            try
            {
                rule = YamlUtility.Deserialize<ConfigFilterRule>(filterDocumentFilePath);
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"Error parsing filter config file {filterDocumentFilePath}: {e.Message}");
            }

            if (rule == null)
            {
                throw new InvalidDataException($"Unable to deserialize filter config {filterDocumentFilePath}.");
            }
            return rule;
        }

        /// <summary>
        ///     This method is responsible to load a configuration filtering rule for APIs with the default definition.
        ///     When the user provides a valic filter document, its rules will superseed the default rules.
        /// </summary>
        /// <param name="filterDocumentFilePath">
        ///     The filter document's path.
        /// </param>
        /// <returns>
        ///     A loaded configuration filtering rule.
        /// </returns>
        public static ConfigFilterRule LoadDefaultWithUserRules(string filterDocumentFilePath)
        {
            ConfigFilterRule defaultRule;

            var assembly = Assembly.GetExecutingAssembly();
            var defaultConfigPath = $"{assembly.GetName().Name}.Filters.defaultfilterconfig.yml";
            using (var stream = assembly.GetManifestResourceStream(defaultConfigPath))
            using (var reader = new StreamReader(stream))
            {
                defaultRule = YamlUtility.Deserialize<ConfigFilterRule>(reader);
            }

            if (string.IsNullOrEmpty(filterDocumentFilePath))
            {
                return defaultRule;
            }

            DidUserProvideFilterDocument = true;
            ConfigFilterRule userRule = LoadFilteringRule(filterDocumentFilePath);

            return new ConfigFilterRule
            {
                // user rule always overwrite default rule
                ApiRules = userRule.ApiRules.Concat(defaultRule.ApiRules),
                AttributeRules = userRule.AttributeRules.Concat(defaultRule.AttributeRules),
            };
        }

        /// <summary>
        ///     This method is responsible to validate whether or not it is possible to extract the metadata of a type or its members.
        /// </summary>
        /// <param name="ruleItems">
        ///     The defined user rules.
        /// </param>
        /// <param name="symbol">
        ///     The symbol to analyze.
        /// </param>
        /// <returns>
        ///     When the method returns <see langword="true"/>, DocFX is able to extract the metadata of the API.
        /// </returns>
        private static bool CanVisitCore(IEnumerable<ConfigFilterRuleItemUnion> ruleItems, SymbolFilterData symbol)
        {
            return DidUserProvideFilterDocument
                ? IsPossibleToVisitSymbolWithUserRule(ruleItems.Where(item => item.Rule != null), symbol)
                : ruleItems.Where(ruleUnion => ruleUnion.Rule != null).Select(ruleUnion => ruleUnion.Rule).Any(rule => rule.IsMatch(symbol) && rule.CanVisit);
        }

        /// <summary>
        ///     This method is responsible to determine whether or not it's possible to visit the APIs that match the user's rules even when it's not a type but only the type's members.
        /// </summary>
        /// <param name="apiRules">
        ///     The API rules given by the user.
        /// </param>
        /// <param name="filterData">
        ///     The symbol that needs to be analyze.
        /// </param>
        /// <returns>
        ///     When the method returns <see langword="true"/>, DocFX is able to extract the metadata of the API.
        /// </returns>
        private static bool IsPossibleToVisitSymbolWithUserRule(IEnumerable<ConfigFilterRuleItemUnion> apiRules, SymbolFilterData filterData)
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

                if (!inputString.Contains(RegexStartOfStringAnchor) && !inputString.Contains(EscapedSpecialCharacterInNamespacePath))
                {
                    // NOT a UID Regex.
                    return inputString;
                }

                return inputString.Replace(RegexStartOfStringAnchor, string.Empty).Replace(EscapedSpecialCharacterInNamespacePath, string.Empty);
            }

            bool DoesApiRuleIdMatchesSymbol(ConfigFilterRuleItem apiRule)
            {
                var apiIdentification = GetCleanUidString(apiRule?.UidRegex);
                return apiIdentification != null && !string.IsNullOrWhiteSpace(filterData.Id) && apiIdentification.Contains(filterData.Id);
            }

            return apiRules.FirstOrDefault(apiRule => DoesApiRuleIdMatchesSymbol(apiRule.Rule)) != null;
        }
    }
}
