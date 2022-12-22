// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DocAsCode.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode
{
    /// <summary>
    /// Provides access to a set of documentations
    /// and their associated configs, compilations and models.
    /// </summary>
    public class Docset
    {
        /// <summary>
        /// Builds a docset specified by docfx.json config.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <returns>A task to await for build completion.</returns>
        public static Task Build(string configPath)
        {
            var consoleLogListener = new ConsoleLogListener();
            var aggregatedLogListener = new AggregatedLogListener();
            Logger.RegisterListener(consoleLogListener);
            Logger.RegisterListener(aggregatedLogListener);

            try
            {
                using var _ = new PerformanceScope(string.Empty, LogLevel.Info);

                var config = JObject.Parse(File.ReadAllText(configPath));
                if (config.TryGetValue("metadata", out var value))
                    RunMetadata.Exec(value.ToObject<MetadataJsonConfig>(JsonUtility.DefaultSerializer.Value), Path.GetDirectoryName(_configPath));
                if (config.TryGetValue("merge", out value))
                    RunMerge.Exec(value.ToObject<MergeJsonConfig>(JsonUtility.DefaultSerializer.Value));
                if (config.TryGetValue("pdf", out value))
                    RunPdf.Exec(value.ToObject<PdfJsonConfig>(JsonUtility.DefaultSerializer.Value));
                if (config.TryGetValue("build", out value))
                    RunBuild.Exec(value.ToObject<BuildJsonConfig>(JsonUtility.DefaultSerializer.Value));

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
