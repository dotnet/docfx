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
            var glob = FindDocsetsGlob(workingDirectory);
            if (glob is null)
            {
                return new[] { (workingDirectory, options.Output) };
            }

            // TODO: look for docfx.json after config migration tool has been merged info docfx
            return (
                from file in Directory.GetFiles(workingDirectory, "docfx.yml", SearchOption.AllDirectories)
                let configPath = Path.GetRelativePath(workingDirectory, file)
                where glob(configPath)
                let docsetPath = Path.GetDirectoryName(file)
                let docsetFolder = Path.GetRelativePath(workingDirectory, docsetPath)
                let outputPath = string.IsNullOrEmpty(options.Output) ? null : Path.Combine(options.Output, docsetFolder)
                select (docsetPath, outputPath)).Distinct().ToArray();
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public (List<Error> errors, Config config) Load(string docsetPath, CommandLineOptions options, bool noFetch = false)
        {
            var configPath = PathUtility.FindYamlOrJson(docsetPath, "docfx");
            if (configPath is null)
            {
                throw Errors.ConfigNotFound(docsetPath).ToException();
            }

            var errors = new List<Error>();

            // Load configs available locally
            var envConfig = LoadEnvironmentVariables();
            var cliConfig = options?.ToJObject();
            var docfxConfig = LoadConfig(errors, Path.GetFileName(configPath), File.ReadAllText(configPath));
            var opsConfig = OpsConfigLoader.LoadAsDocfxConfig(docsetPath, _repository?.Branch ?? "master");
            var globalConfig = File.Exists(AppData.GlobalConfigPath)
                ? LoadConfig(errors, AppData.GlobalConfigPath, File.ReadAllText(AppData.GlobalConfigPath))
                : null;

            // Preload
            var preloadConfigObject = new JObject();
            JsonUtility.Merge(preloadConfigObject, envConfig, globalConfig, opsConfig, docfxConfig, cliConfig);
            var (preloadErrors, preloadConfig) = JsonUtility.ToObject<PreloadConfig>(preloadConfigObject);
            errors.AddRange(preloadErrors);

            // Download dependencies
            var fileResolver = new FileResolver(docsetPath, preloadConfig, noFetch);
            var extendConfig = DownloadExtendConfig(errors, preloadConfig, fileResolver);
            var opsServiceConfig = opsConfig is null ? null : OpsConfigAdapter.Load(fileResolver, preloadConfig.Name, _repository?.Remote, _repository?.Branch);

            // Create full config
            var configObject = new JObject();
            JsonUtility.Merge(configObject, envConfig, globalConfig, opsServiceConfig, extendConfig, opsConfig, docfxConfig, cliConfig);
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

        private JObject DownloadExtendConfig(List<Error> errors, PreloadConfig preloadConfig, FileResolver fileResolver)
        {
            var result = new JObject();

            foreach (var extend in preloadConfig.Extend)
            {
                var content = fileResolver.ReadString(extend);
                var extendConfigObject = LoadConfig(errors, extend, content);
                JsonUtility.Merge(result, extendConfigObject);
            }

            return result;
        }

        private static Func<string, bool> FindDocsetsGlob(string workingDirectory)
        {
            var opsConfig = OpsConfigLoader.LoadOpsConfig(workingDirectory);
            if (opsConfig != null && opsConfig.DocsetsToPublish.Length > 0)
            {
                return docsetFolder =>
                {
                    var sourceFolder = new PathString(Path.GetDirectoryName(docsetFolder));
                    return opsConfig.DocsetsToPublish.Any(docset => docset.BuildSourceFolder.FolderEquals(sourceFolder));
                };
            }

            var configPath = PathUtility.FindYamlOrJson(workingDirectory, "docsets");
            if (configPath != null)
            {
                var content = File.ReadAllText(configPath);
                var source = new FilePath(Path.GetFileName(configPath));
                var config = configPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                    ? YamlUtility.Deserialize<DocsetsConfig>(content, source)
                    : JsonUtility.Deserialize<DocsetsConfig>(content, source);

                return GlobUtility.CreateGlobMatcher(config.Docsets, config.Exclude);
            }

            return null;
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
