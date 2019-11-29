// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ConfigLoader
    {
        private const string Extend = "extend";
        private const string DefaultLocale = "defaultLocale";
        private const string Localization = "localization";

        private readonly string _docsetPath;
        private readonly Input _input;
        private readonly RepositoryProvider _repositoryProvider;

        public ConfigLoader(string docsetPath, Input input, RepositoryProvider repositoryProvider)
        {
            _docsetPath = docsetPath;
            _input = input;
            _repositoryProvider = repositoryProvider;
        }

        public static (string docsetPath, string outputPath)[] FindDocsets(string workingDirectory, CommandLineOptions options)
        {
            if (!Directory.Exists(workingDirectory))
            {
                return Array.Empty<(string, string)>();
            }

            return Directory.GetFiles(workingDirectory, "docfx.yml", SearchOption.AllDirectories)

                // TODO: look for docfx.json after config migration tool has been merged info docfx
                // .Concat(Directory.GetFiles(workingDirectory, "docfx.json", SearchOption.AllDirectories))
                .Select(file => Path.GetDirectoryName(file))
                .Distinct()
                .Select(docsetPath =>
                {
                    var docsetFolder = Path.GetRelativePath(workingDirectory, docsetPath);
                    var outputPath = string.IsNullOrEmpty(options.Output) ? null : Path.Combine(options.Output, docsetFolder);
                    return (docsetPath, outputPath);
                })
                .ToArray();
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public (List<Error> errors, Config config) Load(CommandLineOptions options, bool extend = true)
        {
            if (!TryGetConfigPath(out _))
            {
                throw Errors.ConfigNotFound(_docsetPath).ToException();
            }

            return TryLoad(options, extend);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/> or return default config
        /// </summary>
        public (List<Error> errors, Config config) TryLoad(CommandLineOptions options, bool extend = true)
            => LoadCore(options, extend);

        internal static JObject ExpandVariables(string objectSeparator, string wordSeparator, IEnumerable<(string key, string value)> variables)
        {
            var result = new JObject();

            foreach (var (key, value) in variables)
            {
                var current = result;
                var objects = key.Split(objectSeparator);

                for (var i = 0; i < objects.Length; i++)
                {
                    var name = ToCamelCase(wordSeparator, objects[i]);
                    if (i == objects.Length - 1)
                    {
                        if (current.ContainsKey(name))
                        {
                            if (current[name] is JArray arr)
                            {
                                arr.Add(value);
                            }
                            else
                            {
                                current[name] = new JArray(current[name], value);
                            }
                        }
                        else
                        {
                            current[name] = value;
                        }
                    }
                    else
                    {
                        if (!(current[name] is JObject obj))
                        {
                            current[name] = obj = new JObject();
                        }
                        current = obj;
                    }
                }
            }

            return result;
        }

        private bool TryGetConfigPath(out FilePath configPath)
        {
            configPath = _input.FindYamlOrJson(FileOrigin.Default, "docfx");
            return configPath != null;
        }

        private (List<Error>, Config) LoadCore(CommandLineOptions options, bool extend)
        {
            var errors = new List<Error>();
            var configObject = new JObject();

            var repository = _repositoryProvider.GetRepository(FileOrigin.Default);

            // apply .openpublishing.publish.config.json
            if (OpsConfig.TryLoad(_docsetPath, repository?.Branch ?? "master", out var opsConfig))
            {
                JsonUtility.Merge(configObject, opsConfig);
            }

            // apply docfx.json or docfx.yml
            if (TryGetConfigPath(out var configPath))
            {
                var (mainErrors, mainConfigObject) = LoadConfigObject(configPath.Path, _input.ReadString(configPath));
                errors.AddRange(mainErrors);
                JsonUtility.Merge(configObject, mainConfigObject);
            }

            // apply command line options
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
                (extendErrors, configObject) = ExtendConfigs(configObject);
                errors.AddRange(extendErrors);
            }

            // apply overwrite
            OverwriteConfig(configObject, LocalizationUtility.GetLocale(repository, options), repository?.Branch);

            var (deserializeErrors, config) = JsonUtility.ToObject<Config>(configObject);
            errors.AddRange(deserializeErrors);

            return (errors, config);
        }

        private (List<Error>, JObject) ExtendConfigs(JObject config)
        {
            var result = new JObject();
            var errors = new List<Error>();
            var extends = config[Extend] is JArray arr ? arr : new JArray(config[Extend]);

            foreach (var extend in extends)
            {
                if (extend is JValue value && value.Value is string str)
                {
                    var content = RestoreFileMap.ReadString(
                        _input, new SourceInfo<string>(str, JsonUtility.GetSourceInfo(value)));
                    var (extendErrors, extendConfigObject) = LoadConfigObject(str, content);
                    errors.AddRange(extendErrors);
                    JsonUtility.Merge(result, extendConfigObject);
                }
            }

            JsonUtility.Merge(result, config);
            return (errors, result);
        }

        private static (List<Error>, JObject) LoadConfigObject(string fileName, string content)
        {
            var source = new FilePath(fileName);
            var (errors, config) = fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                ? YamlUtility.Parse(content, source)
                : JsonUtility.Parse(content, source);

            if (config is JObject obj)
            {
                // For v2 backward compatibility, treat `build` section as config if it exist
                if (obj.TryGetValue("build", out var build) && build is JObject buildObj)
                {
                    // `template` property has different semantic, so remove it
                    buildObj.Remove("template");
                    return (errors, buildObj);
                }
                return (errors, obj);
            }

            throw Errors.UnexpectedType(new SourceInfo(source, 1, 1), JTokenType.Object, config.Type).ToException();
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

        private static JObject LoadFromEnvironmentVariables()
        {
            var items = from entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                        let key = entry.Key.ToString()
                        where key.StartsWith("DOCFX_")
                        select (key, entry.Value.ToString());

            return ExpandVariables("__", "_", items);
        }

        private static string ToCamelCase(string wordSeparator, string value)
        {
            var sb = new StringBuilder();
            var words = value.ToLowerInvariant().Split(wordSeparator);
            sb.Length = 0;
            sb.Append(words[0]);
            for (var i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(words[i][0]));
                    sb.Append(words[i], 1, words[i].Length - 1);
                }
            }
            return sb.ToString().Trim();
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
