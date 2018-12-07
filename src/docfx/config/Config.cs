// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal sealed class Config
    {
        private static readonly string[] s_defaultInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultExclude = new[] { "_site/**/*", "localization/**/*" };

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public readonly string Product = string.Empty;

        /// <summary>
        /// Gets the default docset name
        /// </summary>
        public readonly string Name = string.Empty;

        /// <summary>
        /// Gets the file glob patterns included by the docset.
        /// </summary>
        public readonly string[] Files = s_defaultInclude;

        /// <summary>
        /// Gets the file glob patterns excluded from this docset.
        /// </summary>
        public readonly string[] Exclude = s_defaultExclude;

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public readonly OutputConfig Output = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly JObject GlobalMetadata = new JObject();

        /// <summary>
        /// The hostname
        /// </summary>
        public readonly string BaseUrl = string.Empty;

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        public readonly string[] Extend = Array.Empty<string>();

        /// <summary>
        /// Gets the file metadata added to each document.
        /// It is a map of `{metadata-name} -> {glob} -> {metadata-value}`
        /// </summary>
        public readonly Dictionary<string, Dictionary<string, JToken>> FileMetadata = new Dictionary<string, Dictionary<string, JToken>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public readonly Dictionary<string, string> Routes = new Dictionary<string, string>();

        /// <summary>
        /// Gets the configuration about contribution scenario.
        /// </summary>
        public readonly ContributionConfig Contribution = new ContributionConfig();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets the redirection mappings
        /// The default value is empty mappings
        /// The redirection always transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> Redirections = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// <summary>
        /// Gets the redirection mappings without document id
        /// The default value is empty mappings
        /// The redirection doesn't transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> RedirectionsWithoutId = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public readonly DocumentIdConfig DocumentId = new DocumentIdConfig();

        /// <summary>
        /// Gets the rules for error levels by error code.
        /// </summary>
        public readonly Dictionary<string, ErrorLevel> Rules = new Dictionary<string, ErrorLevel>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public readonly Dictionary<string, HttpConfig> Http = new Dictionary<string, HttpConfig>();

        /// <summary>
        /// Gets the configurations related to GitHub APIs, usually related to resolve contributors.
        /// </summary>
        public readonly GitHubConfig GitHub = new GitHubConfig();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        public readonly string[] Xref = Array.Empty<string>();

        /// <summary>
        /// The configurations for localization build
        /// </summary>
        public readonly LocalizationConfig Localization = new LocalizationConfig();

        /// <summary>
        /// Gets the moniker range mapping
        /// </summary>
        public readonly Dictionary<string, string> MonikerRange = new Dictionary<string, string>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public readonly string MonikerDefinition = string.Empty;

        /// <summary>
        /// Get the theme repo url like https://github.com/docs/theme#master
        /// It's used for legacy doc(docs.com) sites build
        /// </summary>
        public readonly string Theme = string.Empty;

        /// <summary>
        /// Gets the config file name.
        /// </summary>
        [JsonIgnore]
        public string ConfigFileName { get; private set; }

        public IEnumerable<string> GetFileReferences()
        {
            foreach (var url in Xref)
            {
                yield return url;
            }

            yield return Contribution.GitCommitsTime;
            yield return GitHub.UserCache;
            yield return MonikerDefinition;
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static (List<Error> errors, Config config) Load(string docsetPath, CommandLineOptions options, bool extend = true)
        {
            if (!TryGetConfigPath(docsetPath, out var configPath, out var configFileName))
            {
                throw Errors.ConfigNotFound(docsetPath, configFileName).ToException();
            }
            var (errors, config) = LoadCore(docsetPath, configPath, options, extend);
            config.ConfigFileName = configFileName;
            return (errors, config);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/>
        /// </summary>
        /// <returns>Whether config exists under <paramref name="docsetPath"/></returns>
        public static bool LoadIfExists(string docsetPath, CommandLineOptions options, out List<Error> errors, out Config config, bool extend = true)
        {
            var exists = TryGetConfigPath(docsetPath, out var configPath, out var configFile);
            if (exists)
            {
                (errors, config) = LoadCore(docsetPath, configPath, options, extend);
            }
            else
            {
                errors = new List<Error>();
                config = new Config();
            }
            return exists;
        }

        public static bool TryGetConfigPath(string parentPath, out string configPath, out string configFile)
        {
            configFile = "docfx.yml";
            configPath = PathUtility.NormalizeFile(Path.Combine(parentPath, configFile));
            if (File.Exists(configPath))
            {
                return true;
            }

            configFile = "docfx.json";
            configPath = PathUtility.NormalizeFile(Path.Combine(parentPath, configFile));
            if (File.Exists(configPath))
            {
                return true;
            }
            return false;
        }

        private static (List<Error>, Config) LoadCore(string docsetPath, string configPath, CommandLineOptions options, bool extend)
        {
            // Options should be converted to config and overwrite the config parsed from docfx.yml/docfx.json
            var errors = new List<Error>();
            Config config = null;

            var (loadErrors, configObject) = LoadConfigObject(configPath, configPath);
            var optionConfigObject = Expand(options?.ToJObject());
            var finalConfigObject = JsonUtility.Merge(configObject, optionConfigObject);

            var globalErrors = new List<Error>();
            (globalErrors, finalConfigObject) = ApplyGlobalConfig(finalConfigObject);
            errors.AddRange(globalErrors);

            if (extend)
            {
                var extendErrors = new List<Error>();
                (extendErrors, finalConfigObject) = ExtendConfigs(finalConfigObject, docsetPath);
                errors.AddRange(extendErrors);
            }

            finalConfigObject = OverwriteConfig(finalConfigObject, options.Locale, GetBranch());

            var deserializeErrors = new List<Error>();
            (deserializeErrors, config) = JsonUtility.ToObjectWithSchemaValidation<Config>(finalConfigObject);
            errors.AddRange(deserializeErrors);

            return (errors, config);

            string GetBranch()
            {
                var repoPath = GitUtility.FindRepo(docsetPath);

                return repoPath == null ? null : GitUtility.GetRepoInfo(repoPath).branch;
            }
        }

        private static (List<Error>, JObject) LoadConfigObject(string fileName, string filePath)
        {
            var errors = new List<Error>();
            JObject config = null;
            if (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = YamlUtility.DeserializeWithSchemaValidation<JObject>(File.ReadAllText(filePath));
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                (errors, config) = JsonUtility.DeserializeWithSchemaValidation<JObject>(File.ReadAllText(filePath));
            }
            return (errors, Expand(config ?? new JObject()));
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

            result.Merge(config, JsonUtility.MergeSettings);
            return (errors, result);
        }

        private static (List<Error>, JObject) ExtendConfigs(JObject config, string docsetPath)
        {
            var result = new JObject();
            var errors = new List<Error>();

            if (config[ConfigConstants.Extend] is JArray extends)
            {
                foreach (var extend in extends)
                {
                    if (extend is JValue value && value.Value is string str)
                    {
                        var filePath = RestoreMap.GetFileRestorePath(docsetPath, str);
                        var (extendErros, extendConfigObject) = LoadConfigObject(str, filePath);
                        errors.AddRange(extendErros);
                        result.Merge(extendConfigObject, JsonUtility.MergeSettings);
                    }
                }
            }

            result.Merge(config, JsonUtility.MergeSettings);
            return (errors, result);
        }

        private static JObject OverwriteConfig(JObject config, string locale, string branch)
        {
            if (string.IsNullOrEmpty(locale))
            {
                if (config.TryGetValue<JObject>(ConfigConstants.Localization, out var localizationConfig) &&
                    localizationConfig.TryGetValue<JValue>(ConfigConstants.DefaultLocale, out var defaultLocale))
                {
                    locale = defaultLocale.Value<string>();
                }
                else
                {
                    locale = LocalizationConfig.DefaultLocaleStr;
                }
            }

            var result = new JObject();
            var overwriteConfigIdentifiers = new List<string>();
            result.Merge(config, JsonUtility.MergeSettings);
            foreach (var (key, value) in config)
            {
                if (OverwriteConfigIdentifier.TryMatch(key, out var identifier))
                {
                    if ((identifier.Branches.Count == 0 || (!string.IsNullOrEmpty(branch) && identifier.Branches.Contains(branch))) &&
                        (identifier.Locales.Count == 0 || (!string.IsNullOrEmpty(locale) && identifier.Locales.Contains(locale))) &&
                        value is JObject overwriteConfig)
                    {
                        result.Merge(Expand(overwriteConfig), JsonUtility.MergeSettings);
                    }

                    overwriteConfigIdentifiers.Add(key);
                }
            }

            // clean up overwrite configuration
            foreach (var overwriteConfigIdentifier in overwriteConfigIdentifiers)
            {
                result.Remove(overwriteConfigIdentifier);
            }

            return result;
        }

        private static JObject Expand(JObject config)
        {
            config[ConfigConstants.Files] = ExpandStringArray(config[ConfigConstants.Files]);
            config[ConfigConstants.Exclude] = ExpandStringArray(config[ConfigConstants.Exclude]);
            config[ConfigConstants.Extend] = ExpandStringArray(config[ConfigConstants.Extend]);
            return config;
        }

        private static JArray ExpandStringArray(JToken e)
        {
            if (e == null)
                return null;
            if (e is JValue str)
                return new JArray(e);
            if (e is JArray arr)
                return arr;
            return null;
        }
    }
}
