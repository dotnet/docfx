// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        [Locale]
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
        public readonly string SiteBasePath = ".";

        /// <summary>
        /// Just for backward compatibility, the source path prefix
        /// </summary>
        public readonly string SourceBasePath = ".";

        /// <summary>
        /// Just for backward compatibility, Indicate that whether generate pdf url template in medadata.
        /// </summary>
        public readonly bool NeedGeneratePdfUrlTemplate = false;

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
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets whether links to redirected files are resolved as redirected url
        /// </summary>
        public readonly bool FollowRedirect = true;

        /// <summary>
        /// Gets the redirection mappings
        /// The default value is empty mappings
        /// The redirection always transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> Redirections = new Dictionary<string, string>(PathUtility.PathComparer);

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
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        public IEnumerable<string> GetExternalReferences()
        {
            yield return Contribution.GitCommitsTime;
            yield return Contribution.UserProfileCache;
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static (List<Error>, Config) Load(string docsetPath, CommandLineOptions options, bool extend = true, RestoreMap restoreMap = null)
        {
            var configPath = PathUtility.NormalizeFile(Path.Combine(docsetPath, "docfx.yml"));
            if (!File.Exists(configPath))
            {
                throw Errors.ConfigNotFound(docsetPath).ToException();
            }
            return LoadCore(docsetPath, configPath, options, extend, restoreMap);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/>
        /// </summary>
        /// <returns>Whether config exists under <paramref name="docsetPath"/></returns>
        public static bool LoadIfExists(string docsetPath, CommandLineOptions options, out List<Error> errors, out Config config, bool extend = true, RestoreMap restoreMap = null)
        {
            var configPath = Path.Combine(docsetPath, "docfx.yml");
            var exists = File.Exists(configPath);
            if (exists)
            {
                (errors, config) = LoadCore(docsetPath, configPath, options, extend, restoreMap);
            }
            else
            {
                errors = new List<Error>();
                config = new Config();
            }
            return exists;
        }

        private static (List<Error>, Config) LoadCore(string docsetPath, string configPath, CommandLineOptions options, bool extend, RestoreMap restoreMap)
        {
            // Options should be converted to config and overwrite the config parsed from docfx.yml
            var errors = new List<Error>();
            Config config = null;
            var (loadErrors, configObject) = LoadConfigObject(configPath);
            var optionConfigObject = ExpandAndNormalize(options?.ToJObject());
            var finalConfigObject = JsonUtility.Merge(configObject, optionConfigObject);

            if (extend)
            {
                var extendErros = new List<Error>();
                (extendErros, finalConfigObject) = ExtendConfigs(finalConfigObject, docsetPath, restoreMap ?? new RestoreMap(docsetPath));
                errors.AddRange(extendErros);
            }

            var deserializeErrors = new List<Error>();
            (deserializeErrors, config) = JsonUtility.ToObject<Config>(finalConfigObject, docsetPath);
            errors.AddRange(deserializeErrors);
            return (errors, config);
        }

        private static (List<Error>, JObject) LoadConfigObject(string filePath)
        {
            var (errors, config) = YamlUtility.Deserialize<JObject>(File.ReadAllText(filePath));
            return (errors, ExpandAndNormalize(config ?? new JObject()));
        }

        private static (List<Error>, JObject) ExtendConfigs(JObject config, string docsetPath, RestoreMap restoreMap)
        {
            var result = new JObject();
            var errors = new List<Error>();

            var configExtend = Environment.GetEnvironmentVariable("DOCFX_CONFIG_EXTEND");
            if (!string.IsNullOrEmpty(configExtend))
            {
                var filePath = restoreMap.GetUrlRestorePath(docsetPath, configExtend);
                (errors, result) = LoadConfigObject(filePath);
            }

            if (config[ConfigConstants.Extend] is JArray extends)
            {
                foreach (var extend in extends)
                {
                    if (extend is JValue value && value.Value is string str)
                    {
                        var filePath = restoreMap.GetUrlRestorePath(docsetPath, str);
                        var (extendErros, extendConfigObject) = LoadConfigObject(filePath);
                        errors.AddRange(extendErros);
                        result.Merge(extendConfigObject, JsonUtility.MergeSettings);
                    }
                }
            }

            result.Merge(config, JsonUtility.MergeSettings);
            return (errors, result);
        }

        private static JObject ExpandAndNormalize(JObject config)
        {
            config[ConfigConstants.Content] = ExpandFiles(config[ConfigConstants.Content]);
            config[ConfigConstants.FileMetadata] = ExpandGlobConfigs(config[ConfigConstants.FileMetadata]);
            config[ConfigConstants.Routes] = ExpandRouteConfigs(config[ConfigConstants.Routes]);
            config[ConfigConstants.Extend] = ExpandStringArray(config[ConfigConstants.Extend]);
            config[ConfigConstants.Redirections] = NormalizeRedirections(config[ConfigConstants.Redirections]);
            config[ConfigConstants.RedirectionsWithoutId] = NormalizeRedirections(config[ConfigConstants.RedirectionsWithoutId]);
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
