// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1401:FieldsMustBePrivate", Justification = "<Skipping>")]
    internal class CommandLineOptions
    {
        public string? Output;
        public string? Log;
        public bool Verbose;
        public OutputType? OutputType;
        public bool DryRun;
        public bool NoDrySync;
        public bool Stdin;
        public bool Force;
        public bool NoCache;
        public bool NoRestore;
        public bool LanguageServer;
        public string? Template;
        public string? TemplateBasePath;

        public JObject? StdinConfig;

        public JObject ToJObject()
        {
            var config = new JObject
            {
                ["dryRun"] = DryRun,
                ["noDrySync"] = NoDrySync,
            };

            if (Output != null)
            {
                config["outputPath"] = Path.GetFullPath(Output);
            }

            if (OutputType != null)
            {
                config["outputType"] = OutputType.Value.ToString();
            }

            if (Template != null)
            {
                config["template"] = Path.GetFullPath(Template);
            }

            if (TemplateBasePath != null)
            {
                config["templateBasePath"] = TemplateBasePath;
            }

            return config;
        }
    }
}
