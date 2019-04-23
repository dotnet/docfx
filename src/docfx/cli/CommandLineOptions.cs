// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public bool Legacy;
        public bool NoRestore;
        public bool Verbose;
        public string Locale;
        public int Port;

        public JObject ToJObject()
        {
            var output = new JObject
            {
                ["path"] = Output != null ? (JValue)Output : JValue.CreateNull(),
            };

            if (Legacy)
            {
                output["json"] = true;
                output["copyResources"] = false;
            }

            return new JObject
            {
                ["output"] = output,
            };
        }
    }
}
