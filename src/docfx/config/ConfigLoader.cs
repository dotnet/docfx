// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ConfigLoader
    {
        private readonly ErrorLog _errorLog;

        public ConfigLoader(ErrorLog errorLog)
        {
            _errorLog = errorLog;
        }

        public static (List<Error> errors, (string docsetPath, string? outputPath)[]) FindDocsets(
            string workingDirectory, CommandLineOptions options)
        {
            var (errors, glob) = FindDocsetsGlob(workingDirectory);
            if (glob is null)
            {
                return (errors, new[] { (workingDirectory, options.Output) });
            }

            var files = new FileSystemEnumerable<string>(
                workingDirectory,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                new EnumerationOptions { RecurseSubdirectories = true })
            {
                ShouldRecursePredicate = (ref FileSystemEntry entry) => entry.FileName[0] != '.',
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                {
                    return !entry.IsDirectory && (
                        entry.FileName.Equals("docfx.json", StringComparison.OrdinalIgnoreCase) ||
                        entry.FileName.Equals("docfx.yml", StringComparison.OrdinalIgnoreCase));
                },
            };

            return (errors, (
                from file in files
                let configPath = Path.GetRelativePath(workingDirectory, file)
                where glob(configPath)
                let docsetPath = Path.GetDirectoryName(file)
                let docsetFolder = Path.GetRelativePath(workingDirectory, docsetPath)
                let outputPath = string.IsNullOrEmpty(options.Output) ? null : Path.Combine(options.Output, docsetFolder)
                select (docsetPath, outputPath)).Distinct().ToArray());
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public (List<Error>, Config, BuildOptions, PackageResolver, FileResolver) Load(
            DisposableCollector disposables, string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
        {
            // load and trace entry repository
            var repository = Repository.Create(docsetPath);
            Telemetry.SetRepository(repository?.Remote, repository?.Branch);

            var configPath = PathUtility.FindYamlOrJson(docsetPath, "docfx");
            if (configPath is null)
            {
                throw Errors.Config.ConfigNotFound(docsetPath).ToException();
            }

            var errors = new List<Error>();
            var unionProperties = new string[] { "xref" };

            // Load configs available locally
            var envConfig = LoadEnvironmentVariables();
            var cliConfig = new JObject();
            JsonUtility.Merge(unionProperties, cliConfig, options.StdinConfig, options.ToJObject());
            var docfxConfig = LoadConfig(errors, Path.GetFileName(configPath), File.ReadAllText(configPath));
            var (opsConfigErrors, xrefEndpoint, xrefQueryTags, opsConfig) = OpsConfigLoader.LoadDocfxConfig(docsetPath, repository);
            errors.AddRange(opsConfigErrors);
            var globalConfig = AppData.TryGetGlobalConfigPath(out var globalConfigPath)
                ? LoadConfig(errors, globalConfigPath, File.ReadAllText(globalConfigPath))
                : null;

            // Preload
            var preloadConfigObject = new JObject();
            JsonUtility.Merge(unionProperties, preloadConfigObject, envConfig, globalConfig, opsConfig, docfxConfig, cliConfig);
            var (preloadErrors, preloadConfig) = JsonUtility.ToObject<PreloadConfig>(preloadConfigObject);
            errors.AddRange(preloadErrors);

            // Download dependencies
            var credentialProvider = preloadConfig.GetCredentialProvider();
            var configAdapter = new OpsConfigAdapter(_errorLog, credentialProvider);
            var packageResolver = new PackageResolver(docsetPath, preloadConfig, fetchOptions);
            disposables.Add(packageResolver);

            var fallbackDocsetPath = LocalizationUtility.GetFallbackDocsetPath(docsetPath, repository, packageResolver);
            var fileResolver = new FileResolver(docsetPath, fallbackDocsetPath, credentialProvider, configAdapter, fetchOptions);
            var buildOptions = new BuildOptions(docsetPath, fallbackDocsetPath, outputPath, repository, preloadConfig);
            var extendConfig = DownloadExtendConfig(
                errors, buildOptions.Locale, preloadConfig, xrefEndpoint, xrefQueryTags, repository, fileResolver);

            // Create full config
            var configObject = new JObject();
            JsonUtility.Merge(unionProperties, configObject, envConfig, globalConfig, extendConfig, opsConfig, docfxConfig, cliConfig);
            var (configErrors, config) = JsonUtility.ToObject<Config>(configObject);
            errors.AddRange(configErrors);

            return (errors, config, buildOptions, packageResolver, fileResolver);
        }

        private static JObject LoadConfig(List<Error> errorBuilder, string fileName, string content)
        {
            var source = new FilePath(fileName);
            var (errors, config) = fileName.EndsWith(".yml", PathUtility.PathComparison)
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

            throw Errors.JsonSchema.UnexpectedType(new SourceInfo(source, 1, 1), JTokenType.Object, config.Type).ToException();
        }

        private JObject DownloadExtendConfig(
            List<Error> errors,
            string? locale,
            PreloadConfig config,
            string? xrefEndpoint,
            string[]? xrefQueryTags,
            Repository? repository,
            FileResolver fileResolver)
        {
            var result = new JObject();
            var extendQuery =
                $"name={WebUtility.UrlEncode(config.Name)}" +
                $"&locale={WebUtility.UrlEncode(locale)}" +
                $"&repository_url={WebUtility.UrlEncode(repository?.Remote)}" +
                $"&branch={WebUtility.UrlEncode(repository?.Branch)}" +
                $"&xref_endpoint={WebUtility.UrlEncode(xrefEndpoint)}" +
                $"&xref_query_tags={WebUtility.UrlEncode(xrefQueryTags is null ? null : string.Join(',', xrefQueryTags))}";

            foreach (var extend in config.Extend)
            {
                var extendWithQuery = extend;
                if (UrlUtility.IsHttp(extend))
                {
                    extendWithQuery = new SourceInfo<string>(UrlUtility.MergeUrl(extend, extendQuery), extend);
                }

                var content = fileResolver.ReadString(extendWithQuery);
                var extendConfigObject = LoadConfig(errors, extend, content);
                JsonUtility.Merge(result, extendConfigObject);
            }

            return result;
        }

        private static (List<Error>, Func<string, bool>?) FindDocsetsGlob(string workingDirectory)
        {
            var errors = new List<Error>();
            var (opsConfigErrors, opsConfig) = OpsConfigLoader.LoadOpsConfig(workingDirectory);
            errors.AddRange(opsConfigErrors);
            if (opsConfig != null && opsConfig.DocsetsToPublish.Length > 0)
            {
                return (errors, docsetFolder =>
                {
                    var docsetDirectoryName = Path.GetDirectoryName(docsetFolder);
                    if (docsetDirectoryName is null)
                    {
                        return false;
                    }
                    var sourceFolder = new PathString(docsetDirectoryName);
                    return opsConfig.DocsetsToPublish.Any(docset => docset.BuildSourceFolder.FolderEquals(sourceFolder));
                });
            }

            var configPath = PathUtility.FindYamlOrJson(workingDirectory, "docsets");
            if (configPath != null)
            {
                var content = File.ReadAllText(configPath);
                var source = new FilePath(Path.GetFileName(configPath));

                var (configErrors, config) = configPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                    ? YamlUtility.Deserialize<DocsetsConfig>(content, source)
                    : JsonUtility.Deserialize<DocsetsConfig>(content, source);
                errors.AddRange(configErrors);

                return (errors, GlobUtility.CreateGlobMatcher(config.Docsets, config.Exclude));
            }

            return (errors, null);
        }

        private static JObject LoadEnvironmentVariables()
        {
            return new JObject(
                from entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                let key = entry.Key.ToString()
                where key.StartsWith("DOCFX_")
                let configKey = StringUtility.ToCamelCase('_', key.Substring("DOCFX_".Length))
                let values = entry.Value?.ToString()?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                where values != null
                let configValue = values.Length == 1 ? (object)values[0] : values
                select new JProperty(configKey, configValue));
        }
    }
}
