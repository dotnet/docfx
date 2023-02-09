// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Dotnet;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode
{
    internal static class RunMetadata
    {
        static RunMetadata()
        {
            var vs = MSBuildLocator.RegisterDefaults() ?? throw new DocfxException(
                $"Cannot find a supported .NET Core SDK. Install .NET Core SDK {Environment.Version.Major}.{Environment.Version.Minor}.x to build .NET API docs.");

            Logger.LogInfo($"Using {vs.Name} {vs.Version}");
        }

        public static async Task Exec(MetadataJsonConfig config, string configDirectory, string outputDirectory = null)
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

                        var options = ConvertToOptions(item, outputDirectory ?? configDirectory);

                        EnvironmentContext.SetGitFeaturesDisabled(item.DisableGitFeatures);

                        // TODO: Use plugin to generate metadata for files with different extension?
                        using var worker = new ExtractMetadataWorker(options);
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

        private static ExtractMetadataOptions ConvertToOptions(MetadataJsonItemConfig configModel, string outputDirectory)
        {
            var projects = configModel.Source;
            var references = configModel.References;
            var outputFolder = configModel.Destination ?? Constants.DefaultMetadataOutputFolderName;

            var expandedFiles = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, projects);
            var expandedReferences = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, references);

            return new ExtractMetadataOptions
            {
                PreserveRawInlineComments = configModel?.Raw ?? false,
                ShouldSkipMarkup = configModel?.ShouldSkipMarkup ?? false,
                FilterConfigFile = configModel?.FilterConfigFile is null ? null : Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, configModel.FilterConfigFile)),
                GlobalNamespaceId = configModel?.GlobalNamespaceId,
                UseCompatibilityFileName = configModel?.UseCompatibilityFileName ?? false,
                MSBuildProperties = configModel?.MSBuildProperties,
                OutputFolder = Path.GetFullPath(Path.Combine(outputDirectory, outputFolder)),
                CodeSourceBasePath = configModel?.CodeSourceBasePath,
                DisableDefaultFilter = configModel?.DisableDefaultFilter ?? false,
                TocNamespaceStyle = configModel?.TocNamespaceStyle ?? TocNamespaceStyle.Flattened,
                Files = expandedFiles.Items.SelectMany(s => s.Files).ToList(),
                References = expandedReferences?.Items.SelectMany(s => s.Files).ToList(),
            };
        }
    }
}
