// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ConfigLoader
    {
        public static (string docsetPath, string? outputPath)[] FindDocsets(ErrorBuilder errors, string workingDirectory, CommandLineOptions options)
        {
            var glob = FindDocsetsGlob(errors, workingDirectory);
            if (glob is null)
            {
                return new[] { (workingDirectory, options.Output) };
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

            return (
                from file in files
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
        public static (Config, BuildOptions, PackageResolver, FileResolver, OpsAccessor) Load(
            ErrorBuilder errors, DisposableCollector disposables, string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
        {
            // load and trace entry repository
            var repository = Repository.Create(docsetPath);
            Telemetry.SetRepository(repository?.Remote, repository?.Branch);

            var docfxConfig = LoadConfig(errors, docsetPath);
            if (docfxConfig is null)
            {
                throw Errors.Config.ConfigNotFound(docsetPath).ToException();
            }

            var unionProperties = new string[] { "xref" };

            // Load configs available locally
            var envConfig = LoadEnvironmentVariables();
            var cliConfig = new JObject();
            JsonUtility.Merge(unionProperties, cliConfig, options.StdinConfig, options.ToJObject());
            var (xrefEndpoint, xrefQueryTags, opsConfig) = OpsConfigLoader.LoadDocfxConfig(errors, docsetPath, repository);

            var globalConfig = LoadConfig(errors, AppData.Root);

            // Preload
            var preloadConfigObject = new JObject();
            JsonUtility.Merge(unionProperties, preloadConfigObject, envConfig, globalConfig, opsConfig, docfxConfig, cliConfig);
            var preloadConfig = JsonUtility.ToObject<PreloadConfig>(errors, preloadConfigObject);

            // Download dependencies
            var credentialProvider = preloadConfig.GetCredentialProvider();
            var opsAccessor = new OpsAccessor(errors, credentialProvider);
            var configAdapter = new OpsConfigAdapter(opsAccessor);
            var packageResolver = new PackageResolver(docsetPath, preloadConfig, fetchOptions, repository);
            disposables.Add(packageResolver);

            var fallbackDocsetPath = LocalizationUtility.GetFallbackDocsetPath(docsetPath, repository, preloadConfig.FallbackRepository, packageResolver);
            var fileResolver = new FileResolver(docsetPath, fallbackDocsetPath, credentialProvider, configAdapter, fetchOptions);
            var buildOptions = new BuildOptions(docsetPath, fallbackDocsetPath, outputPath, repository, preloadConfig);
            var extendConfig = DownloadExtendConfig(errors, buildOptions.Locale, preloadConfig, xrefEndpoint, xrefQueryTags, repository, fileResolver);

            // Create full config
            var configObject = new JObject();
            JsonUtility.Merge(unionProperties, configObject, envConfig, globalConfig, extendConfig, opsConfig, docfxConfig, cliConfig);
            var config = JsonUtility.ToObject<Config>(errors, configObject);

            Telemetry.TrackDocfxConfig(config.Name, docfxConfig);
            return (config, buildOptions, packageResolver, fileResolver, opsAccessor);
        }

        private static JObject? LoadConfig(ErrorBuilder errors, string directory)
        {
            var config = PathUtility.LoadYamlOrJson<JObject>(errors, directory, "docfx");
            if (config is null)
            {
                return null;
            }

            // For v2 backward compatibility, treat `build` section as config if it exist
            if (config.TryGetValue("build", out var build) && build is JObject buildObj)
            {
                // `template` property has different semantic, so remove it
                buildObj.Remove("template");
                return buildObj;
            }

            return config;
        }

        private static JObject DownloadExtendConfig(
            ErrorBuilder errors,
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
                $"&branch={WebUtility.UrlEncode(repository?.Branch ?? "main")}" +
                $"&xref_endpoint={WebUtility.UrlEncode(xrefEndpoint)}" +
                $"&xref_query_tags={WebUtility.UrlEncode(xrefQueryTags is null ? null : string.Join(',', xrefQueryTags))}";

            foreach (var extend in config.Extend)
            {
                var extendWithQuery = extend;
                if (UrlUtility.IsHttp(extend))
                {
                    extendWithQuery = new SourceInfo<string>(UrlUtility.MergeUrl(extend, extendQuery), extend);
                }

                var extendContent = fileResolver.ReadString(extendWithQuery);
                var extendConfigObject = extend.Value.EndsWith(".yml", PathUtility.PathComparison)
                    ? YamlUtility.Deserialize<JObject>(errors, extendContent, new FilePath(extend))
                    : JsonUtility.Deserialize<JObject>(errors, extendContent, new FilePath(extend));

                JsonUtility.Merge(result, extendConfigObject);
            }

            return result;
        }

        private static Func<string, bool>? FindDocsetsGlob(ErrorBuilder errors, string workingDirectory)
        {
            var opsConfig = OpsConfigLoader.LoadOpsConfig(errors, workingDirectory);
            if (opsConfig != null && opsConfig.DocsetsToPublish.Length > 0)
            {
                return docsetFolder =>
                {
                    var docsetDirectoryName = Path.GetDirectoryName(docsetFolder);
                    if (docsetDirectoryName is null)
                    {
                        return false;
                    }
                    var sourceFolder = new PathString(docsetDirectoryName);
                    return opsConfig.DocsetsToPublish.Any(docset => docset.BuildSourceFolder.FolderEquals(sourceFolder));
                };
            }

            var config = PathUtility.LoadYamlOrJson<DocsetsConfig>(errors, workingDirectory, "docsets");
            if (config != null)
            {
                return GlobUtility.CreateGlobMatcher(config.Docsets, config.Exclude);
            }

            return null;
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
