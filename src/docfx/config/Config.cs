// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs
{
    internal class Config
    {
        public string Locale { get; } = "en-us";

        public FileConfig Files { get; } = new FileConfig();

        public JObject Metadata { get; } = new JObject();

        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            return null;
        }
    }
}
