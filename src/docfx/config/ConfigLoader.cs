// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ConfigLoader
    {
        private readonly string _docsetPath;
        private readonly RestoreFileMap _restoreFileMap;
        private readonly Repository _repository;

        public ConfigLoader(string docsetPath, RestoreFileMap restoreFileMap, Repository repository)
        {
            _docsetPath = docsetPath;
            _restoreFileMap = restoreFileMap;
            _repository = repository;
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

        private bool TryGetConfigPath(out string configPath)
        {
            configPath = PathUtility.FindYamlOrJson(Path.Combine(_docsetPath, "docfx"));
            return configPath != null;
        }

        private (List<Error>, Config) LoadCore(CommandLineOptions options, bool extend)
        {
            var errors = new List<Error>();
            var configObject = new JObject();

            // apply .openpublishing.publish.config.json
            if (OpsConfigLoader.TryLoad(_docsetPath, _repository?.Branch ?? "master", out var opsConfig))
            {
                JsonUtility.Merge(configObject, opsConfig);
            }

            // apply docfx.json or docfx.yml
            if (TryGetConfigPath(out var configPath))
            {
                var (mainErrors, mainConfigObject) = LoadConfigObject(Path.GetFileName(configPath), File.ReadAllText(configPath));
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

            var (deserializeErrors, config) = JsonUtility.ToObject<Config>(configObject);
            errors.AddRange(deserializeErrors);

            return (errors, config);
        }

        private (List<Error>, JObject) ExtendConfigs(JObject config)
        {
            var result = new JObject();
            var errors = new List<Error>();
            var extends = config["extend"] is JArray arr ? arr : new JArray(config["extend"]);

            foreach (var extend in extends)
            {
                if (extend is JValue value && value.Value is string str)
                {
                    var content = _restoreFileMap.ReadString(
                        new SourceInfo<string>(str, JsonUtility.GetSourceInfo(value)));
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
    }
}
