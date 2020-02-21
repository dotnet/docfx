// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output { get; set; }

        public bool Legacy { get; set; }

        public bool Verbose { get; set; }

        public bool DryRun { get; set; }

        public bool Stdin { get; set; }

        public bool NoRestore { get; set; }

        public string Template { get; set; }

        public int Port { get; set; }

        public bool UseCache { get; set; }

        public JObject StdinConfig { get; set; }

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
