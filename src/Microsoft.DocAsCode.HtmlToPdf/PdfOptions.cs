﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    public class PdfOptions
    {
        public string SourceDirectory { get; set; }

        public string DestDirectory { get; set; }

        public string PdfDocsetName { get; set; }

        public string CssFilePath { get; set; }

        public string LoadErrorHandling { get; set; }

        public string[] ExcludeTocs { get; set; }

        public bool GenerateAppendices { get; set; } = false;

        public string Host { get; set; }

        public string BasePath { get; set; }

        public string Locale { get; set; }

        public bool NeedGeneratePdfExternalLink { get; set; } = false;

        public bool KeepRawFiles { get; set; } = false;

        public bool ExcludeDefaultToc { get; set; } = false;

        public int PdfConvertParallelism { get; set; } = 4;

        public string ExternalLinkFormat => $"{Normalize(Host)}{Normalize(Locale)}{Normalize(BasePath)}{{0}}";

        /// <summary>
        /// Gets or sets the path and file name of the pdf executable.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Specify additional command line arguments that should be passed to the pdf command.
        /// </summary>
        public string AdditionalPdfCommandArgs { get; set; }

        /// <summary>
        /// Gets or sets the "Table of Contents" bookmark title.
        /// </summary>
        public string TocTitle { get; set; } = "Table of Contents";

        /// <summary>
        /// Gets or sets the outline option.
        /// </summary>
        public OutlineOption OutlineOption { get; set; } = OutlineOption.DefaultOutline;

        /// <summary>
        /// Gets or sets the cover page title.
        /// </summary>
        public string CoverPageTitle { get; set; } = "Cover Page";

        /// <summary>
        /// If the path only with '/' or null or empty, will skip and return empty. Others will trim and return with as 'a/'
        /// </summary>
        /// <param name="path">the path</param>
        /// <returns>the normalized path</returns>
        private string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            path = path.Trim('/');
            return path?.Length == 0 ? path : path + "/";
        }

        /// <summary>
        /// Are input arguments set using command line
        /// </summary>
        public bool NoInputStreamArgs { get; set; }
    }
}
