// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public string Log;
        public string BuildLocale;
        public bool Stable;
        public IReadOnlyList<string> GitHubTokens;

        public JObject ToJObject() => new JObject
        {
            ["output"] = new JObject
            {
                ["path"] = Output != null ? (JValue)Output : JValue.CreateNull(),
                ["logPath"] = Log != null ? (JValue)Log : JValue.CreateNull(),
                ["stable"] = Stable ? (JValue)true : JValue.CreateNull(),
            },
            ["locale"] = BuildLocale != null ? (JValue)BuildLocale : JValue.CreateNull(),
            ["github"] = new JObject { ["token"] = GitHubTokens != null ? JToken.FromObject(GitHubTokens) : JValue.CreateNull() },
        };
    }
}
