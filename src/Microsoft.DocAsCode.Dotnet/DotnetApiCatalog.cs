// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Plugins;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.Dotnet
{
    /// <summary>
    /// Provides access to a .NET API definitions and their associated documentation.
    /// </summary>
    public static class DotnetApiCatalog
    {
        static DotnetApiCatalog()
        {
            var vs = MSBuildLocator.RegisterDefaults() ?? throw new ExtractMetadataException(
                $"Cannot find a supported .NET Core SDK. Install .NET Core SDK {Environment.Version.Major}.{Environment.Version.Minor}.x to build .NET API docs.");

            Logger.LogInfo($"Using {vs.Name} {vs.Version}");
        }

        /// <summary>
        /// Generates metadata reference YAML files using docfx.json config.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <returns>A task to await for build completion.</returns>
        public static Task GenerateManagedReferenceYamlFiles(string configPath)
        {
            return GenerateManagedReferenceYamlFiles(configPath, new());
        }

        /// <summary>
        /// Generates metadata reference YAML files using docfx.json config.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <returns>A task to await for build completion.</returns>
        public static async Task GenerateManagedReferenceYamlFiles(string configPath, DotnetApiOptions options)
        {
            var consoleLogListener = new ConsoleLogListener();
            Logger.RegisterListener(consoleLogListener);

            try
            {
                var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));

                var config = JObject.Parse(File.ReadAllText(configPath));
                if (config.TryGetValue("metadata", out var value))
                    await Exec(value.ToObject<MetadataJsonConfig>(JsonUtility.DefaultSerializer.Value), options, configDirectory);
            }
            finally
            {
                Logger.Flush();
                Logger.PrintSummary();
                Logger.UnregisterAllListeners();
            }
        }

        internal static async Task Exec(MetadataJsonConfig config, DotnetApiOptions options, string configDirectory, string outputDirectory = null)
        {
            try
            {
                using (new LoggerPhaseScope("ExtractMetadata"))
                {
                    string originalGlobalNamespaceId = VisitorHelper.GlobalNamespaceId;

                    EnvironmentContext.SetBaseDirectory(configDirectory);

                    foreach (var item in config)
                    {
                        VisitorHelper.GlobalNamespaceId = item.GlobalNamespaceId;
                        EnvironmentContext.SetGitFeaturesDisabled(item.DisableGitFeatures);

                        // TODO: Use plugin to generate metadata for files with different extension?
                        using var worker = new ExtractMetadataWorker(ConvertConfig(item, outputDirectory ?? configDirectory), options);
                        await worker.ExtractMetadataAsync();
                    }

                    VisitorHelper.GlobalNamespaceId = originalGlobalNamespaceId;
                }
            }
            catch (AggregateException e)
            {
                throw e.GetBaseException();
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        private static ExtractMetadataConfig ConvertConfig(MetadataJsonItemConfig configModel, string outputDirectory)
        {
            var projects = configModel.Source;
            var references = configModel.References;
            var outputFolder = configModel.Destination ?? "_api";

            var expandedFiles = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, projects);
            var expandedReferences = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, references);

            return new ExtractMetadataConfig
            {
                PreserveRawInlineComments = configModel?.Raw ?? false,
                ShouldSkipMarkup = configModel?.ShouldSkipMarkup ?? false,
                FilterConfigFile = configModel?.FilterConfigFile is null ? null : Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, configModel.FilterConfigFile)),
                IncludePrivateMembers = configModel?.IncludePrivateMembers ?? false,
                GlobalNamespaceId = configModel?.GlobalNamespaceId,
                UseCompatibilityFileName = configModel?.UseCompatibilityFileName ?? false,
                MSBuildProperties = configModel?.MSBuildProperties,
                OutputFolder = Path.GetFullPath(Path.Combine(outputDirectory, outputFolder)),
                CodeSourceBasePath = configModel?.CodeSourceBasePath,
                DisableDefaultFilter = configModel?.DisableDefaultFilter ?? false,
                NamespaceLayout = configModel?.NamespaceLayout ?? NamespaceLayout.Flattened,
                Files = expandedFiles.Items.SelectMany(s => s.Files).ToList(),
                References = expandedReferences?.Items.SelectMany(s => s.Files).ToList(),
            };
        }
    }
}
