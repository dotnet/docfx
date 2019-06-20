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

        [Option("generatesAppendices", HelpText = "Specify whether or not to generate appendices for not-in-TOC articles")]
        public bool? GeneratesAppendices { get; set; }

        [Option("generatesExternalLink", HelpText = "Specify whether or not to generate external links for PDF")]
        public bool? GeneratesExternalLink { get; set; }

        [Option("keepRawFiles", HelpText = "Specify whether or not to keep the intermediate html files that used to generate the PDF file. It it usually used in debug purpose. By default the value is false")]
        public bool? KeepRawFiles { get; set; }

        [Option("errorHandling", HelpText = "Specify how to handle pdf pages that fail to load: abort, ignore or skip(default abort), it is the same input as wkhtmltopdf --load-error-handling options")]
        public string LoadErrorHandling { get; set; }

        [Option("rawOutputFolder", HelpText = "Specify the output folder for the raw files, if not specified, raw files will by default be saved to _raw subfolder under output folder if keepRawFiles is set to true.")]
        public string RawOutputFolder { get; set; }

        [Option("host", HelpText = "Specify the hostname to link not-in-TOC articles")]
        public new string Host { get; set; }

        [Option("locale", HelpText = "Specify the locale of the pdf file")]
        public string Locale { get; set; }

        [Option("excludedTocs", HelpText = "Specify the toc files to be excluded")]
        public ListWithStringFallback ExcludedTocs { get; set; }

        [Option("basePath", HelpText = "Specify the base path to generate external link, {host}/{locale}/{basePath}")]
        public string BasePath { get; set; }

        [Option("noStdin", HelpText = "Do not use stdin when wkhtmltopdf is executed")]
        public bool? NoInputStreamArgs { get; set; }

    }
}
