// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Config
    {
        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public string Locale { get; } = "en-us";

        /// <summary>
        /// Gets the files that are managed by this docset.
        /// </summary>
        public FileConfig Files { get; } = new FileConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public JObject Metadata { get; } = new JObject();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            return null;
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
