// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Config
    {
        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public readonly string Locale = "en-us";

        /// <summary>
        /// Gets the files that are managed by this docset.
        /// </summary>
        public readonly FileConfig Files = new FileConfig();

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public readonly OutputConfig Output = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly JObject Metadata = new JObject();

        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            var configPath = Path.Combine(docsetPath, "docfx.yml");

            return YamlUtility.Deserialize<Config>(File.ReadAllText(configPath));
        }
    }
}
