// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Text;

    public class HtmlToPdfOptions
    {
        private const string DefaultStyleSheet = "defaults/default-css.css";

        /// <summary>
        /// Specify a user style sheet, to load with every page.
        /// </summary>
        public string UserStyleSheet { get; set; }

        /// <summary>
        /// The option to put a outline into the pdf (default put a default outline into the pdf).
        /// </summary>
        public OutlineOption OutlineOption { get; set; } = OutlineOption.DefaultOutline;

        /// <summary>
        /// Set the default text encoding, for input
        /// </summary>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>
        /// Be less verbose if true.
        /// </summary>
        public bool IsQuiet { get; set; } = true;

        /// <summary>
        /// Whether read command line arguments from stdin or not (default read command line arguments from stdin).
        /// </summary>
        public bool IsReadArgsFromStdin { get; set; } = true;

        /// <summary>
        /// Whether output the stream to stdout or not (default output the stream to stdout).
        /// </summary>
        public bool IsOutputToStdout { get; set; } = true;

        /// <summary>
        /// The base path or base url.
        /// </summary>
        public string BasePath { get; set; }

        /// <summary>
        /// The header html path for pdf.
        /// </summary>
        public string HeaderHtmlPath { get; set; }

        /// <summary>
        /// The footer html path for pdf.
        /// </summary>
        public string FooterHtmlPath { get; set; }

        /// <summary>
        /// Specify how to handle pages that fail to load: abort, ignore or skip(default abort)
        /// </summary>
        public string LoadErrorHandling { get; set; } = "skip";

        /// <summary>
        /// When convert pdf failed, we will retry twice, the default interval value is 5 seconds.
        /// </summary>
        public TimeSpan[] RetryIntervals { get; set; } = new[] { TimeSpan.FromSeconds(5) };

        /// <summary>
        /// The max degree of parallelism to convert pdf.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 8;

        /// <summary>
        /// Additional arguments.
        /// </summary>
        public string AdditionalArguments { get; set; }

        /// <summary>
        /// Get the string of the html to pdf options.
        /// </summary>
        /// <returns>The configuration of html to pdf options.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // here to show the options http://wkhtmltopdf.org/usage/wkhtmltopdf.txt
            // why set javascript delay to 3000? https://github.com/wkhtmltopdf/wkhtmltopdf/issues/2054
            sb.Append(" --javascript-delay 3000");
            sb.Append(IsQuiet ? " -q" : string.Empty);
            sb.Append(OutlineOption == OutlineOption.WkDefaultOutline ? " --outline" : " --no-outline");
            if (!string.IsNullOrEmpty(Encoding))
            {
                sb.Append(" --encoding ").Append(Encoding);
            }
            if (string.IsNullOrEmpty(UserStyleSheet))
            {
                sb.Append(" --user-style-sheet \"").Append(DefaultStyleSheet).Append("\"");
            }
            else
            {
                sb.Append(" --user-style-sheet \"").Append(UserStyleSheet).Append("\"");
            }

            if (!string.IsNullOrEmpty(HeaderHtmlPath))
            {
                sb.Append(" --header-html \"").Append(HeaderHtmlPath).Append("\"");
            }
            if (!string.IsNullOrEmpty(FooterHtmlPath))
            {
                sb.Append(" --footer-html \"").Append(FooterHtmlPath).Append("\"");
            }
            if (!string.IsNullOrEmpty(LoadErrorHandling))
            {
                sb.Append(" --load-error-handling ").Append(LoadErrorHandling);
            }
            if (IsReadArgsFromStdin)
            {
                sb.Append(" --read-args-from-stdin");
            }
            if (!string.IsNullOrEmpty(AdditionalArguments))
            {
                sb.Append(" ").Append(AdditionalArguments);
            }

            return sb.ToString();
        }
    }
}
