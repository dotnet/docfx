// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            // Options should be converted to config and overwrite the config parsed from docfx.yml
            var configPath = Path.Combine(docsetPath, "docfx.yml");

            if (!File.Exists(configPath))
                throw new DocumentException("config-not-found", $"Cannot find docfx.yml at '{configPath}'");

            return YamlUtility.Deserialize<Config>(File.ReadAllText(configPath));
        }

        public static bool TryLoad(string docsetPath, CommandLineOptions options, out Config config)
        {
            try
            {
                config = Load(docsetPath, options);
            }
            catch
            {
                // TODO: error handling
                config = null;
            }

            return config != null;
        }
    }
}
