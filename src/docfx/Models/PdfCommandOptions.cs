// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    [OptionUsage("pdf [<config file path>]")]
    internal class PdfCommandOptions : BuildCommandOptions
    {
        [Option("name", HelpText = "Specify the name of the generated pdf")]
        public string Name { get; set; }

        [Option("appendices", HelpText = "Specify whether or not generate appendices for not-in-TOC articles")]
        public bool GenerateAppendices { get; set; }

        [Option("external", HelpText = "Specify whether or not generate external links for PDF")]
        public bool GeneratePdfExternalLink { get; set; }

        [Option("host", HelpText = "Specify the hostname to link not-in-TOC articles")]
        public string Host { get; set; }

        [Option("locale", HelpText = "Specify the locale of the pdf file")]
        public string Locale { get; set; }
    }
}
