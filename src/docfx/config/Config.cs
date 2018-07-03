// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Config
    {
        private static readonly string[] s_defaultContentInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultContentExclude = new[] { "_site/**/*" };

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public readonly string Product = string.Empty;

        /// <summary>
        /// Gets the default docset name
        /// </summary>
        public readonly string Name = string.Empty;

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public readonly string Locale = "en-us";

        /// <summary>
        /// Gets the contents that are managed by this docset.
        /// </summary>
        public readonly FileConfig Content = new FileConfig(s_defaultContentInclude, s_defaultContentExclude);

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public readonly OutputConfig Output = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly JObject GlobalMetadata = new JObject();

        /// <summary>
        /// Just for backward compatibility, the output site path prefix
        /// </summary>
        public readonly string SiteBasePath = string.Empty;

        /// <summary>
        /// Just for backward compatibility, the source path prefix
        /// </summary>
        public readonly string SourceBasePath = string.Empty;

        /// <summary>
        /// Just for backward compatibility, Indicate that whether generate pdf url template in medadata.
        /// </summary>
        public readonly bool NeedGeneratePdfUrlTemplate = false;

        /// <summary>
        /// The hostname
        /// </summary>
        public readonly string BaseUrl = string.Empty;

        /// <summary>
        /// Gets the file metadata added to each document.
        /// </summary>
        public readonly GlobConfig<JObject>[] FileMetadata = Array.Empty<GlobConfig<JObject>>();

        /// <summary>
        /// Gets the input and output path mapping configuration of documents.
        /// </summary>
        public readonly RouteConfig[] Routes = Array.Empty<RouteConfig>();

        /// <summary>
        /// Gets the configuration about contribution scenario.
        /// </summary>
        public readonly ContributionConfig Contribution = new ContributionConfig();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the redirection mappings
        /// The default value is empty mappings
        /// The redirection always transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> Redirections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the redirection mappings without document id
        /// The default value is empty mappings
        /// The redirection doesn't transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> RedirectionsWithoutId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public readonly DocumentIdConfig DocumentId = new DocumentIdConfig();

        /// <summary>
        /// Gets the rules for error levels by error code.
        /// </summary>
        public readonly Dictionary<string, ErrorLevel> Rules = new Dictionary<string, ErrorLevel>();

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            var configPath = PathUtility.NormalizeFile(Path.Combine(docsetPath, "docfx.yml"));
            if (!File.Exists(configPath))
            {
                throw Errors.ConfigNotFound(docsetPath).ToException();
            }
            return LoadCore(configPath, options);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/>
        /// </summary>
        /// <returns>Whether config exists under <paramref name="docsetPath"/></returns>
        public static bool LoadIfExists(string docsetPath, CommandLineOptions options, out Config config)
        {
            var configPath = Path.Combine(docsetPath, "docfx.yml");
            var exists = File.Exists(configPath);
            config = exists ? LoadCore(configPath, options) : new Config();
            return exists;
        }

        private static Config LoadCore(string configPath, CommandLineOptions options = null)
        {
            // Options should be converted to config and overwrite the config parsed from docfx.yml
            Config config = null;
            try
            {
                var configObject = JsonUtility.Merge(
                    ExpandAndNormalize(LoadOriginalConfigObject(configPath, new List<string>(), true)),
                    options?.ToJObject());

                config = configObject.ToObject<Config>(JsonUtility.DefaultDeserializer);
            }
            catch (Exception e)
            {
                throw Errors.InvalidConfig(configPath, e.Message).ToException(e);
            }

            Validate(config, configPath);

            return config;
        }

        private static void Validate(Config config, string configPath)
        {
            ValidateLocale(config);
            ValidateContributorConfig(config.Contribution, configPath);
        }

        private static void ValidateLocale(Config config)
        {
            try
            {
                var culture = new CultureInfo(config.Locale);
            }
            catch (CultureNotFoundException e)
            {
                throw Errors.InvalidLocale(config.Locale).ToException(e);
            }
        }

        private static void ValidateContributorConfig(ContributionConfig config, string configPath)
        {
            if (!string.IsNullOrEmpty(config.UserProfileCachePath)
                && File.Exists(Path.Combine(configPath, config.UserProfileCachePath)))
            {
                throw Errors.UserProfileCacheNotFound(config.UserProfileCachePath).ToException();
            }
            if (!string.IsNullOrEmpty(config.GitCommitsTimePath)
                && File.Exists(Path.Combine(configPath, config.GitCommitsTimePath)))
            {
                throw Errors.UserProfileCacheNotFound(config.GitCommitsTimePath).ToException();
            }
        }

        private static JObject LoadOriginalConfigObject(string configPath, List<string> parents, bool expand)
        {
            // TODO: support URL
            var (errors, _, config) = YamlUtility.Deserialize<JObject>(File.ReadAllText(configPath));
            if (errors.Any())
            {
                throw errors[0].ToException();
            }

            if (config == null)
                config = new JObject();
            if (!expand || !config.TryGetValue(ConfigConstants.Extend, out var objExtend))
                return config;

            if (parents.Contains(configPath))
                throw Errors.CircularReference(configPath, parents).ToException();

            parents.Add(configPath);
            var extendedConfig = new JObject();
            foreach (var path in GetExtendConfigPaths(objExtend))
            {
                var extendConfigPath = PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(configPath), path));
                extendedConfig.Merge(LoadOriginalConfigObject(extendConfigPath, parents, false));
            }
            extendedConfig.Merge(config);
            parents.RemoveAt(parents.Count - 1);

            return extendedConfig;
        }

        private static IEnumerable<string> GetExtendConfigPaths(JToken objExtend)
        {
            if (objExtend == null)
                yield break;
            if (objExtend is JValue strExtend)
            {
                yield return strExtend.Value.ToString();
                yield break;
            }
            if (objExtend is JArray arrExtend)
            {
                foreach (var path in arrExtend)
                {
                    if (!(path is JValue strPath))
                        throw new Exception($"Expect to be string: {JsonUtility.Serialize(path)}");
                    yield return strPath.Value.ToString();
                }
                yield break;
            }
            throw new Exception($"Expect 'extend' to be string or array: {JsonUtility.Serialize(objExtend)}");
        }

        private static JObject ExpandAndNormalize(JObject config)
        {
            config[ConfigConstants.Content] = ExpandFiles(config[ConfigConstants.Content]);
            config[ConfigConstants.FileMetadata] = ExpandGlobConfigs(config[ConfigConstants.FileMetadata]);
            config[ConfigConstants.Routes] = ExpandRouteConfigs(config[ConfigConstants.Routes]);
            config[ConfigConstants.Redirections] = NormalizeRedirections(config[ConfigConstants.Redirections]);
            config[ConfigConstants.RedirectionsWithoutDocumentId] = NormalizeRedirections(config[ConfigConstants.RedirectionsWithoutDocumentId]);
            return config;
        }

        private static JToken NormalizeRedirections(JToken redirection)
        {
            if (redirection == null)
                return null;

            if (redirection is JObject redirectionJObject)
            {
                var normalizedRedirection = new JObject();
                foreach (var (path, redirectTo) in redirectionJObject)
                {
                    var normalizedPath = PathUtility.NormalizeFile(path);
                    normalizedRedirection[normalizedPath] = redirectTo;
                }

                return normalizedRedirection;
            }

            return redirection;
        }

        private static JToken ExpandRouteConfigs(JToken jToken)
        {
            if (jToken == null)
                return null;
            if (!(jToken is JObject obj))
                throw new Exception($"Expect to be an object: {JsonUtility.Serialize(jToken)}");

            var result = new JArray();
            foreach (var (key, value) in obj)
            {
                if (!(value is JValue strValue))
                    throw new Exception($"Expect to be a string: {JsonUtility.Serialize(jToken)}");

                result.Add(new JObject
                {
                    [ConfigConstants.Source] = key.EndsWith('/') || key.EndsWith('\\') ?
                        PathUtility.NormalizeFolder(key) :
                        PathUtility.NormalizeFile(key),
                    [ConfigConstants.Destination] = PathUtility.NormalizeFile(strValue.Value.ToString()),
                });
            }
            return result;
        }

        private static JToken ExpandGlobConfigs(JToken token)
        {
            if (token == null)
                return null;
            if (token is JObject obj)
                token = ToGlobConfigs(obj);
            if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    ExpandGlobConfig(item);
                }
            }
            return token;
        }

        private static void ExpandGlobConfig(JToken item)
        {
            if (!(item is JObject obj))
                throw new Exception($"Expect to be an object: {JsonUtility.Serialize(item)}");
            ExpandIncludeExclude(obj);
        }

        private static JArray ToGlobConfigs(JObject obj)
        {
            var result = new JArray();

            foreach (var (key, value) in obj)
            {
                if (key.Contains("*"))
                    throw new Exception($"Glob is not supported in config key: '{key}'");
                result.Add(new JObject
                {
                    [ConfigConstants.Include] = key,
                    [ConfigConstants.Value] = value,
                    [ConfigConstants.IsGlob] = false,
                });
            }

            return result;
        }

        private static JObject ExpandFiles(JToken file)
        {
            if (file == null)
                return null;
            if (file is JValue str)
                file = new JArray(str);
            if (file is JArray arr)
                file = new JObject { [ConfigConstants.Include] = arr };
            return ExpandIncludeExclude((JObject)file);
        }

        private static JObject ExpandIncludeExclude(JObject item)
        {
            Debug.Assert(item != null);
            item[ConfigConstants.Include] = ExpandStringArray(item[ConfigConstants.Include]);
            item[ConfigConstants.Exclude] = ExpandStringArray(item[ConfigConstants.Exclude]);
            return item;
        }

        private static JArray ExpandStringArray(JToken e)
        {
            if (e == null)
                return null;
            if (e is JValue str)
                return new JArray(e);
            if (e is JArray arr)
                return arr;

            // TODO: error handle
            return null;
        }
    }
}
