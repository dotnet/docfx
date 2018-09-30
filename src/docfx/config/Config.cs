// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal sealed class Config
    {
        private const string DefaultLocaleStr = "en-us";
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
        public readonly string DefaultLocale = DefaultLocaleStr;

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
        /// Gets the authorization keys for required resources access
        /// </summary>
        public readonly HttpConfig Http = new HttpConfig();

        /// <summary>
        /// Gets the configurations related to GitHub APIs, usually related to resolve contributors.
        /// </summary>
        public readonly GitHubConfig GitHub = new GitHubConfig();

        /// <summary>
        /// Gets the configturation related to git repositories, usually used to clone a repo.
        /// </summary>
        public readonly GitConfig Git = new GitConfig();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        /// <summary>
        /// Gets whether to build internal xref map
        /// </summary>
        public readonly bool BuildInternalXrefMap = true;

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        public readonly string[] Xref = Array.Empty<string>();

        public IEnumerable<string> GetExternalReferences()
        {
            foreach (var url in Xref)
            {
                yield return url;
            }

            yield return Contribution.GitCommitsTime;
            yield return GitHub.UserCache;
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
                var extendErrors = new List<Error>();
                (extendErrors, finalConfigObject) = ExtendConfigs(finalConfigObject, docsetPath, restoreMap ?? new RestoreMap(docsetPath));
                errors.AddRange(extendErrors);
            }

            finalConfigObject = OverwriteConfig(finalConfigObject, options.Locale, GetBranch());

            var deserializeErrors = new List<Error>();
            (deserializeErrors, config) = JsonUtility.ToObject<Config>(finalConfigObject);
            errors.AddRange(deserializeErrors);

            return (errors, config);

            string GetBranch()
            {
                var repoPath = GitUtility.FindRepo(docsetPath);

                return repoPath == null ? null : GitUtility.GetRepoInfo(repoPath).branch;
            }
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

            if (File.Exists(AppData.GlobalConfigPath))
            {
                var filePath = restoreMap.GetUrlRestorePath(docsetPath, AppData.GlobalConfigPath);
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

        private static JObject OverwriteConfig(JObject config, string locale, string branch)
        {
            if (string.IsNullOrEmpty(locale))
            {
                if (config.TryGetValue(ConfigConstants.DefaultLocale, out var defaultLocale) && defaultLocale is JValue defaultLocaleValue)
                    locale = defaultLocaleValue.Value<string>();
                else
                    locale = DefaultLocaleStr;
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
                        result.Merge(ExpandAndNormalize(overwriteConfig), JsonUtility.MergeSettings);
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

        private static JToken ExpandRouteConfigs(JToken token)
        {
            if (token is JObject obj)
            {
                var result = new JArray();
                foreach (var (key, value) in obj)
                {
                    result.Add(new JObject
                    {
                        [ConfigConstants.Source] = key.EndsWith('/') || key.EndsWith('\\')
                            ? PathUtility.NormalizeFolder(key)
                            : PathUtility.NormalizeFile(key),
                        [ConfigConstants.Destination] = value is JValue v && v.Value is string str
                            ? PathUtility.NormalizeFile(str)
                            : value,
                    });
                }
                return result;
            }
            return token;
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
            if (item is JObject obj)
            {
                ExpandIncludeExclude(obj);
            }
        }

        private static JArray ToGlobConfigs(JObject obj)
        {
            var result = new JArray();

            foreach (var (key, value) in obj)
            {
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
            return null;
        }
    }
}
