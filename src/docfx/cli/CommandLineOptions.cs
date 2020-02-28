// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1401:FieldsMustBePrivate", Justification = "<Skipping>")]
    internal class CommandLineOptions
    {
        public string? Output;
        public bool Legacy;
        public bool Verbose;
        public bool DryRun;
        public bool Stdin;
        public bool UseCache;
        public bool NoRestore;
        public string? Template;
        public int Port;

        public JObject? StdinConfig;

        public FetchOptions FetchOptions => NoRestore
            ? FetchOptions.NoFetch
            : (UseCache ? FetchOptions.UseCache : FetchOptions.None);

        public JObject ToJObject()
        {
            var config = new JObject
            {
                ["legacy"] = Legacy,
                ["dryRun"] = DryRun,
            };

            if (Output != null)
                config["outputPath"] = Output;

            if (Legacy)
            {
                config["outputJson"] = true;
                config["copyResources"] = false;
            }

            if (Template != null)
                config["template"] = Template;

            return config;
        }
    }
}
