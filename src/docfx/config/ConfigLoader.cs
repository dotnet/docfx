// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ConfigLoader
    {
        private readonly Repository _repository;

        public ConfigLoader(Repository repository)
        {
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
        public (List<Error> errors, Config config) Load(string docsetPath, CommandLineOptions options, bool noFetch = false)
        {
            var configPath = PathUtility.FindYamlOrJson(Path.Combine(docsetPath, "docfx"));
            if (configPath is null)
            {
                throw Errors.ConfigNotFound(docsetPath).ToException();
            }

            var errors = new List<Error>();

            // Load configs available locally
            var envConfig = LoadEnvironmentVariables();
            var cliConfig = options?.ToJObject();
            var docfxConfig = LoadConfig(errors, Path.GetFileName(configPath), File.ReadAllText(configPath));
            var opsConfig = OpsConfigLoader.TryLoad(docsetPath, _repository?.Branch);
            var globalConfig = File.Exists(AppData.GlobalConfigPath)
                ? LoadConfig(errors, AppData.GlobalConfigPath, File.ReadAllText(AppData.GlobalConfigPath))
                : null;

            // Preload
            var preloadConfigObject = new JObject();
            JsonUtility.Merge(preloadConfigObject, envConfig, globalConfig, opsConfig, docfxConfig, cliConfig);
            var (preloadErrors, preloadConfig) = JsonUtility.ToObject<PreloadConfig>(preloadConfigObject);
            errors.AddRange(preloadErrors);

            // Download dependencies
            var fileDownloader = new FileDownloader(docsetPath, preloadConfig, noFetch);
            var extendConfig = DownloadExtendConfig(errors, preloadConfig, fileDownloader);
            var opsServiceConfig = new OpsConfigAdapter(noFetch).TryAdapt(preloadConfig.Name, _repository?.Remote, _repository?.Branch);

            // Create full config
            var configObject = new JObject();
            JsonUtility.Merge(configObject, envConfig, globalConfig, opsConfig, opsServiceConfig, extendConfig, docfxConfig, cliConfig);
            var (configErrors, config) = JsonUtility.ToObject<Config>(configObject);
            errors.AddRange(configErrors);

            return (errors, config);
        }

        private static JObject LoadConfig(List<Error> errorBuilder, string fileName, string content)
        {
            var source = new FilePath(fileName);
            var (errors, config) = fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                ? YamlUtility.Parse(content, source)
                : JsonUtility.Parse(content, source);

            errorBuilder.AddRange(errors);

            if (config is JObject obj)
            {
                // For v2 backward compatibility, treat `build` section as config if it exist
                if (obj.TryGetValue("build", out var build) && build is JObject buildObj)
                {
                    // `template` property has different semantic, so remove it
                    buildObj.Remove("template");
                    return buildObj;
                }
                return obj;
            }

            throw Errors.UnexpectedType(new SourceInfo(source, 1, 1), JTokenType.Object, config.Type).ToException();
        }

        private JObject DownloadExtendConfig(List<Error> errors, PreloadConfig preloadConfig, FileDownloader fileDownloader)
        {
            var result = new JObject();

            foreach (var extend in preloadConfig.Extend)
            {
                var content = fileDownloader.DownloadString(extend);
                var extendConfigObject = LoadConfig(errors, extend, content);
                JsonUtility.Merge(result, extendConfigObject);
            }

            return result;
        }

        private static JObject LoadEnvironmentVariables()
        {
            var items = from entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                        let key = entry.Key.ToString()
                        where key.StartsWith("DOCFX_")
                        let configKey = key.Substring("DOCFX_".Length)
                        let values = entry.Value.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries)
                        from value in values
                        select (configKey, value);

            return StringUtility.ExpandVariables("__", "_", items);
        }
    }
}
