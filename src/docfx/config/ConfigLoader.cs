// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ConfigLoader
    {
        private const string Extend = "extend";
        private const string DefaultLocale = "defaultLocale";
        private const string Localization = "localization";

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static (List<Error> errors, Config config) Load(string docsetPath, CommandLineOptions options, string locale = null, bool extend = true)
        {
            if (!TryGetConfigPath(docsetPath, out _))
            {
                throw Errors.ConfigNotFound(docsetPath).ToException();
            }

            return TryLoad(docsetPath, options, locale, extend);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/> or return default config
        /// </summary>
        public static (List<Error> errors, Config config) TryLoad(string docsetPath, CommandLineOptions options, string locale = null, bool extend = true)
            => LoadCore(docsetPath, options, locale, extend);

        public static bool TryGetConfigPath(string docset, out string configPath)
        {
            configPath = PathUtility.FindYamlOrJson(Path.Combine(docset, "docfx"));

            return !string.IsNullOrEmpty(configPath);
        }

        private static (List<Error>, Config) LoadCore(string docsetPath, CommandLineOptions options, string locale, bool extend)
        {
            if (!TryGetConfigPath(docsetPath, out var configPath))
            {
                return (new List<Error>(), new Config());
            }

            var configFileName = PathUtility.NormalizeFile(Path.GetRelativePath(docsetPath, configPath));
            var (errors, configObject) = LoadConfigObject(configFileName, File.ReadAllText(configPath));

            // apply options
            var optionConfigObject = options?.ToJObject();

            JsonUtility.Merge(configObject, optionConfigObject);

            // apply global config
            var globalErrors = new List<Error>();
            (globalErrors, configObject) = ApplyGlobalConfig(configObject);
            errors.AddRange(globalErrors);

            // apply extends
            if (extend)
            {
                var extendErrors = new List<Error>();
                (extendErrors, configObject) = ExtendConfigs(configObject, docsetPath);
                errors.AddRange(extendErrors);
            }

            // apply overwrite
            OverwriteConfig(configObject, locale ?? options.Locale, GetBranch());

            var (deserializeErrors, config) = JsonUtility.ToObject<Config>(configObject);
            errors.AddRange(deserializeErrors);

            return (errors, config);

            string GetBranch()
            {
                var repoPath = GitUtility.FindRepo(docsetPath);

                return repoPath is null ? null : GitUtility.GetRepoInfo(repoPath).branch;
            }
        }

        private static (List<Error>, JObject) LoadConfigObject(string fileName, string content)
        {
            var errors = new List<Error>();
            JToken config = null;
            if (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = YamlUtility.Parse(content, fileName);
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = JsonUtility.Parse(content, fileName);
            }

            JsonUtility.TrimStringValues(config);

            return (errors, config as JObject ?? new JObject());
        }

        private static (List<Error>, JObject) ApplyGlobalConfig(JObject config)
        {
            var result = new JObject();
            var errors = new List<Error>();

            var globalConfigPath = AppData.GlobalConfigPath;
            if (File.Exists(globalConfigPath))
            {
                (errors, result) = LoadConfigObject(globalConfigPath, File.ReadAllText(globalConfigPath));
            }

            JsonUtility.Merge(result, config);
            return (errors, result);
        }

        private static (List<Error>, JObject) ExtendConfigs(JObject config, string docsetPath)
        {
            var result = new JObject();
            var errors = new List<Error>();
            var extends = config[Extend] is JArray arr ? arr : new JArray(config[Extend]);

            foreach (var extend in extends)
            {
                if (extend is JValue value && value.Value is string str)
                {
                    var (_, content, _) = RestoreMap.GetRestoredFileContent(docsetPath, new SourceInfo<string>(str, JsonUtility.GetSourceInfo(value)));
                    var (extendErrors, extendConfigObject) = LoadConfigObject(str, content);
                    errors.AddRange(extendErrors);
                    JsonUtility.Merge(result, extendConfigObject);
                }
            }

            JsonUtility.Merge(result, config);
            return (errors, result);
        }

        private static void OverwriteConfig(JObject config, string locale, string branch)
        {
            if (string.IsNullOrEmpty(locale))
            {
                if (config.TryGetValue<JObject>(Localization, out var localizationConfig) &&
                    localizationConfig.TryGetValue<JValue>(DefaultLocale, out var defaultLocale))
                {
                    locale = defaultLocale.Value<string>();
                }
                else
                {
                    locale = LocalizationConfig.DefaultLocaleStr;
                }
            }

            var overwriteConfigIdentifiers = new List<string>();
            var overwriteConfigs = new List<JObject>();
            foreach (var (key, value) in config)
            {
                if (OverwriteConfigIdentifier.TryMatch(key, out var identifier))
                {
                    if ((identifier.Branches.Count == 0 || (!string.IsNullOrEmpty(branch) && identifier.Branches.Contains(branch))) &&
                        (identifier.Locales.Count == 0 || (!string.IsNullOrEmpty(locale) && identifier.Locales.Contains(locale))) &&
                        value is JObject overwriteConfig)
                    {
                        overwriteConfigs.Add(overwriteConfig);
                    }

                    overwriteConfigIdentifiers.Add(key);
                }
            }

            foreach (var overwriteConfig in overwriteConfigs)
            {
                JsonUtility.Merge(config, overwriteConfig);
            }

            // clean up overwrite configuration
            foreach (var overwriteConfigIdentifier in overwriteConfigIdentifiers)
            {
                config.Remove(overwriteConfigIdentifier);
            }
        }
    }
}
