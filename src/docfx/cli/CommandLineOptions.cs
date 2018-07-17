// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public string Log;
        public bool Legacy;
        public string Token;

        public JObject ToJObject() => new JObject
        {
            ["output"] = new JObject
            {
                ["path"] = Output != null ? (JValue)Output : JValue.CreateNull(),
                ["logPath"] = Log != null ? (JValue)Log : JValue.CreateNull(),
            },
        };
    }
}
