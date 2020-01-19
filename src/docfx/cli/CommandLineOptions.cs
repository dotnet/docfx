// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public bool Legacy;
        public bool Verbose;
        public bool DryRun;
        public bool Stdin;
        public bool UseCache;
        public bool NoRestore;
        public string Template;
        public int Port;

        public JObject StdinConfig;

        public FetchOptions FetchOptions => NoRestore
            ? FetchOptions.NoFetch
            : (UseCache ? FetchOptions.UseCache : FetchOptions.None);

        public JObject ToJObject()
        {
            var output = new JObject();
            if (Output != null)
                output["path"] = Output;

            if (Legacy)
            {
                output["json"] = true;
                output["copyResources"] = false;
            }

            var config = new JObject
            {
                ["output"] = output,
                ["legacy"] = Legacy,
                ["dryRun"] = DryRun,
            };

            if (Template != null)
                config["template"] = Template;

            return config;
        }
    }
}
