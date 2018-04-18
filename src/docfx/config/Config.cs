// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Gets the output config.
        /// </summary>
        public OutputConfig Output { get; } = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public JObject Metadata { get; } = new JObject();

        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            return new Config();
        }
    }
}
