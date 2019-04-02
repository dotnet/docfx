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
        private const string Files = "files";
        private const string Exclude = "exclude";
        private const string Extend = "extend";
        private const string DefaultLocale = "defaultLocale";
        private const string Localization = "localization";

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static (List<Error> errors, Config config) Load(string docsetPath, CommandLineOptions options, string locale = null, bool extend = true)
        {
            var configPath = PathUtility.FindYamlOrJson(Path.Combine(docsetPath, "docfx"));
            if (configPath is null)
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

        private static (List<Error>, Config) LoadCore(string docsetPath, CommandLineOptions options, string locale,  bool extend)
        {
            var errors = new List<Error>();
            Config config = null;

            var configExists = TryGetConfigPath(docsetPath, out var configPath);
            var (loadErrors, configObject) = !configExists ? (errors, new JObject()) : LoadConfigObject(configPath, configPath);

            // apply options
            var optionConfigObject = Expand(options?.ToJObject());

            JsonUtility.Merge(configObject, optionConfigObject);
            errors.AddRange(loadErrors);

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

            var deserializeErrors = new List<Error>();
            (deserializeErrors, config) = JsonUtility.ToObjectWithSchemaValidation<Config>(configObject);
            errors.AddRange(deserializeErrors);

            config.ConfigFileName = !configExists
                ? config.ConfigFileName
                : PathUtility.NormalizeFile(Path.GetRelativePath(docsetPath, configPath));
            return (errors, config);

            string GetBranch()
            {
                var repoPath = GitUtility.FindRepo(docsetPath);

                return repoPath is null ? null : GitUtility.GetRepoInfo(repoPath).branch;
            }
        }

        private static (List<Error>, JObject) LoadConfigObject(string fileName, string file)
            => LoadConfigObjectContent(fileName, File.ReadAllText(file));

        private static (List<Error>, JObject) LoadConfigObjectContent(string fileName, string content)
        {
            var errors = new List<Error>();
            JToken config = null;
            if (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = YamlUtility.Deserialize(content);
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = JsonUtility.DeserializeWithSchemaValidation<JObject>(content);
            }

            if (config is JObject)
            {
                if (config["globalMetadata"] != null)
                {
                    errors.AddRange(MetadataValidator.ValidateGlobalMetadata(config["globalMetadata"] as JObject));
                }
                else if (config["fileMetadata"] != null)
                {
                    errors.AddRange(MetadataValidator.ValidateFileMetadata(config["fileMetadata"] as JObject));
                }
            }
            return (errors, Expand(config as JObject ?? new JObject()));
        }

        private static (List<Error>, JObject) ApplyGlobalConfig(JObject config)
        {
            var result = new JObject();
            var errors = new List<Error>();

            var globalConfigPath = AppData.GlobalConfigPath;
            if (File.Exists(globalConfigPath))
            {
                (errors, result) = LoadConfigObject(globalConfigPath, globalConfigPath);
            }

            JsonUtility.Merge(result, config);
            return (errors, result);
        }

        private static (List<Error>, JObject) ExtendConfigs(JObject config, string docsetPath)
        {
            var result = new JObject();
            var errors = new List<Error>();

            if (config[Extend] is JArray extends)
            {
                foreach (var extend in extends)
                {
                    if (extend is JValue value && value.Value is string str)
                    {
                        var (_, content, _) = RestoreMap.GetRestoredFileContent(docsetPath, str, default).GetAwaiter().GetResult(); /*todo: remove GetResult()*/
                        var (extendErros, extendConfigObject) = LoadConfigObjectContent(str, content);
                        errors.AddRange(extendErros);
                        JsonUtility.Merge(result, extendConfigObject);
                    }
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
            var expands = new List<JObject>();
            foreach (var (key, value) in config)
            {
                if (OverwriteConfigIdentifier.TryMatch(key, out var identifier))
                {
                    if ((identifier.Branches.Count == 0 || (!string.IsNullOrEmpty(branch) && identifier.Branches.Contains(branch))) &&
                        (identifier.Locales.Count == 0 || (!string.IsNullOrEmpty(locale) && identifier.Locales.Contains(locale))) &&
                        value is JObject overwriteConfig)
                    {
                        expands.Add(Expand(overwriteConfig));
                    }

                    overwriteConfigIdentifiers.Add(key);
                }
            }

            foreach (var expand in expands)
            {
                JsonUtility.Merge(config, expand);
            }

            // clean up overwrite configuration
            foreach (var overwriteConfigIdentifier in overwriteConfigIdentifiers)
            {
                config.Remove(overwriteConfigIdentifier);
            }
        }

        private static JObject Expand(JObject config)
        {
            config[Files] = ExpandStringArray(config[Files]);
            config[Exclude] = ExpandStringArray(config[Exclude]);
            config[Extend] = ExpandStringArray(config[Extend]);
            return config;
        }

        private static JArray ExpandStringArray(JToken e)
        {
            if (e is null)
                return null;
            if (e is JValue str)
                return new JArray(e);
            if (e is JArray arr)
                return arr;
            return null;
        }
    }
}
