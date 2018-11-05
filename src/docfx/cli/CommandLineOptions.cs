// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public bool Legacy;
        public string GitHubToken;
        public string Locale;

        public JObject ToJObject()
        {
            return new JObject
            {
                ["output"] = new JObject
                {
                    ["path"] = Output != null ? (JValue)Output : JValue.CreateNull(),
                    ["json"] = Legacy ? (JValue)true : JValue.CreateNull(),
                    ["copyResources"] = Legacy ? (JValue)false : JValue.CreateNull(),
                },
                ["gitHub"] = new JObject
                {
                    ["authToken"] = GitHubToken != null ? (JValue)GitHubToken : JValue.CreateNull(),
                },
            };
        }
    }
}
