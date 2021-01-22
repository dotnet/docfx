// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ConfigLoader
    {
        public static (string docsetPath, string? outputPath)[] FindDocsets(
            ErrorBuilder errors, Package package, CommandLineOptions options, Repository? repository)
        {
            var glob = FindDocsetsGlob(errors, package, repository);
            if (glob is null)
            {
                return new[] { (package.BasePath.Value, options.Output) };
            }

            var files = package.GetFiles(allowedFileNames: new string[] { "docfx.json", "docfx.yml" });

            return (
                from file in files
                where glob(file)
                let fullPath = package.GetFullFilePath(file)
                let docsetPath = Path.GetDirectoryName(fullPath)
                let docsetFolder = Path.GetDirectoryName(file)
                let outputPath = string.IsNullOrEmpty(options.Output) ? null : Path.Combine(options.Output, docsetFolder)
                select (docsetPath, outputPath)).Distinct().ToArray();
        }

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static (Config, BuildOptions, PackageResolver, FileResolver, OpsAccessor) Load(
            ErrorBuilder errors,
            Repository? repository,
            string docsetPath,
            string? outputPath,
            CommandLineOptions options,
            FetchOptions fetchOptions,
            Package package,
            CredentialProvider? getCredential = null)
        {
            // load and trace entry repository
            Telemetry.SetRepository(repository?.Url, repository?.Branch);

            var docfxConfig = LoadConfig(errors, package);
            if (docfxConfig is null)
            {
                throw Errors.Config.ConfigNotFound(package.BasePath).ToException();
            }

            var unionProperties = new string[] { "xref" };

            // Load configs available locally
            var envConfig = LoadEnvironmentVariables();
            var cliConfig = new JObject();
            JsonUtility.Merge(unionProperties, cliConfig, options.StdinConfig, options.ToJObject());
            var (xrefEndpoint, xrefQueryTags, opsConfig) = OpsConfigLoader.LoadDocfxConfig(errors, repository, package);

            var globalConfig = LoadConfig(errors, package, new PathString(AppData.Root));

            // Preload
            var preloadConfigObject = new JObject();
            JsonUtility.Merge(unionProperties, preloadConfigObject, envConfig, globalConfig, opsConfig, docfxConfig, cliConfig);
            var preloadConfig = JsonUtility.ToObject<PreloadConfig>(errors, preloadConfigObject);

            // Download dependencies
            var credentialProviders = new List<CredentialProvider>();
            if (getCredential != null)
            {
                credentialProviders.Add(getCredential);
            }
            credentialProviders.Add((url, _, _) => Task.FromResult(preloadConfig.GetHttpConfig(url)));
            var credentialHandler = new CredentialHandler(credentialProviders.ToArray());
            var opsAccessor = new OpsAccessor(errors, credentialHandler);
            var configAdapter = new OpsConfigAdapter(opsAccessor);

            PackageResolver? packageResolver = default;
            var fallbackDocsetPath = new Lazy<string?>(
                () => LocalizationUtility.GetFallbackDocsetPath(docsetPath, repository, preloadConfig.FallbackRepository, packageResolver!));
            var fileResolver = new FileResolver(package, fallbackDocsetPath, credentialHandler, configAdapter, fetchOptions);

            packageResolver = new PackageResolver(errors, docsetPath, preloadConfig, fetchOptions, fileResolver, repository);

            var buildOptions = new BuildOptions(docsetPath, fallbackDocsetPath.Value, outputPath, repository, preloadConfig, package);
            var extendConfig = DownloadExtendConfig(errors, buildOptions.Locale, preloadConfig, xrefEndpoint, xrefQueryTags, repository, fileResolver);

            // Create full config
            var configObject = new JObject();
            JsonUtility.Merge(unionProperties, configObject, envConfig, globalConfig, extendConfig, opsConfig, docfxConfig, cliConfig);
            var config = JsonUtility.ToObject<Config>(errors, configObject);

            Telemetry.TrackDocfxConfig(config.Name, docfxConfig);
            return (config, buildOptions, packageResolver, fileResolver, opsAccessor);
        }

        private static JObject? LoadConfig(ErrorBuilder errors, Package package, PathString directory = default)
        {
            var config = package.TryLoadYamlOrJson<JObject>(errors, "docfx", directory);
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
                $"&repository_url={WebUtility.UrlEncode(repository?.Url)}" +
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

        private static Func<string, bool>? FindDocsetsGlob(ErrorBuilder errors, Package package, Repository? repository)
        {
            var opsConfig = OpsConfigLoader.LoadOpsConfig(errors, package, repository);
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

            var config = package.TryLoadYamlOrJson<DocsetsConfig>(errors, "docsets");
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
                let configKey = StringUtility.ToCamelCase('_', key["DOCFX_".Length..])
                let configValue = entry.Value?.ToString()
                where !string.IsNullOrEmpty(configValue)
                select new JProperty(configKey, GetJsonValue(configValue)));
        }

        private static object GetJsonValue(string value)
        {
            try
            {
                return JObject.Parse(value);
            }
            catch (Exception)
            {
            }

            var values = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return values.Length == 1 ? values[0] : values;
        }
    }
}
