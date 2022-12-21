// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.SubCommands;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode
{
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

        public Task Build()
        {
            var config = JObject.Parse(File.ReadAllText(_configPath));
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
    }
}
