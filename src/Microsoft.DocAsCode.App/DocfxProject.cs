// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DocAsCode.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode
{
    /// <summary>
    /// Provides options to be used with <see cref="DocfxProject.Build(BuildOptions)"/>.
    /// </summary>
    public class BuildOptions
    {
        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="BuildOptions"/> that uses the default configuration.
        /// </summary>
        public static BuildOptions Default { get; } = new();

        /// <summary>
        /// Get or sets a value that indicates whether to export model as JSON as part of the build.
        /// </summary>
        public bool Json { get; init; }
    }

    /// <summary>
    /// A docfx project provides access to a set of documentations
    /// and their associated configs, compilations and models.
    /// </summary>
    public class DocfxProject
    {
        /// <summary>
        /// Loads a docfx project from docfx.json.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <returns>The created docfx project.</returns>
        public static DocfxProject Load(string configPath)
        {
            return new DocfxProject(configPath);
        }

        private readonly string _configPath;

        private DocfxProject(string configPath) => _configPath = configPath;

        /// <summary>
        /// Builds the project as if executing the <c>docfx {configPath}</c> command.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <param name="options">Option to control build behavior.</param>
        /// <returns>A task to await for build completion.</returns>
        public static Task Build(string configPath, BuildOptions options = null)
        {
            return new DocfxProject(configPath).Build(options ?? BuildOptions.Default);
        }

        /// <summary>
        /// Builds the project as if executing the <c>docfx {configPath}</c> command.
        /// </summary>
        /// <param name="options">Option to control build behavior.</param>
        /// <returns>A task to await for build completion.</returns>
        public Task Build(BuildOptions options = null)
        {
            options ??= BuildOptions.Default;
            var consoleLogListener = new ConsoleLogListener();
            var aggregatedLogListener = new AggregatedLogListener();
            Logger.RegisterListener(consoleLogListener);
            Logger.RegisterListener(aggregatedLogListener);

            try
            {
                using var _ = new PerformanceScope(string.Empty, LogLevel.Info);

                var config = JObject.Parse(File.ReadAllText(_configPath));
                if (config.TryGetValue("metadata", out var value))
                    RunMetadata.Exec(value.ToObject<MetadataJsonConfig>(JsonUtility.DefaultSerializer.Value), Path.GetDirectoryName(_configPath));
                if (config.TryGetValue("merge", out value))
                    RunMerge.Exec(value.ToObject<MergeJsonConfig>(JsonUtility.DefaultSerializer.Value));
                if (config.TryGetValue("pdf", out value))
                    RunPdf.Exec(value.ToObject<PdfJsonConfig>(JsonUtility.DefaultSerializer.Value));

                if (config.TryGetValue("build", out value))
                {
                    var buildConfig = value.ToObject<BuildJsonConfig>(JsonUtility.DefaultSerializer.Value);
                    if (options.Json)
                        buildConfig.ExportRawModel = true;
                    RunBuild.Exec(buildConfig);
                }

                return Task.CompletedTask;
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }
    }
}
