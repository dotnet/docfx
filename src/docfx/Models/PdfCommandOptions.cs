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

        [Option("css", HelpText = "Specify the path for the css to generate pdf, default value is styles/default.css")]
        public string CssFilePath { get; set; }

        [Option("appendices", HelpText = "Specify whether or not generate appendices for not-in-TOC articles")]
        public bool GenerateAppendices { get; set; }

        [Option("external", HelpText = "Specify whether or not generate external links for PDF")]
        public bool GeneratePdfExternalLink { get; set; }

        [Option("host", HelpText = "Specify the hostname to link not-in-TOC articles")]
        public new string Host { get; set; }

        [Option("locale", HelpText = "Specify the locale of the pdf file")]
        public string Locale { get; set; }

        [Option("excludeTocs", HelpText = "Specify the toc files to be excluded")]
        public ListWithStringFallback ExcludedTocs { get; set; }

        [Option("basePath", HelpText = "Specify the base path to generate external link, {host}/{locale}/{basePath}")]
        public string BasePath { get; set; }
    }
}
