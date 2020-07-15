// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Docs.Build
{
    internal class Options
    {
        [Value(0, Required = true)]
        public string Repository { get; set; } = "";

        [Option("branch")]
        public string Branch { get; set; } = "master";

        [Option("locale")]
        public string Locale { get; set; } = "en-us";

        [Option("timeout")]
        public int? Timeout { get; set; }

        [Option("output-html")]
        public bool OutputHtml { get; set; }

        [Option("dry-run")]
        public bool DryRun { get; set; }

        [Option("markdown-rule-api")]
        public string? MarkdownRuleApi { get; set; }

        [Option("metadata-schema-api")]
        public string? MetadataSchemaApi { get; set; }

        [Option("error-level")]
        public ErrorLevel ErrorLevel { get; set; }
    }
}
