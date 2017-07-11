// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public class Options
    {
        public Func<string, string, string> Highlight { get; set; }

        public Func<string, string> Sanitizer { get; set; }

        public string LangPrefix { get; set; } = "lang-";

        public string HeaderPrefix { get; set; } = string.Empty;

        public bool XHtml { get; set; }

        public bool Sanitize { get; set; }

        public bool Pedantic { get; set; }

        public bool Mangle { get; set; } = true;

        public bool Smartypants { get; set; }

        public bool Breaks { get; set; }

        public bool Gfm { get; set; } = true;

        public bool Tables { get; set; } = true;

        public bool SmartLists { get; set; }

        public bool ShouldExportSourceInfo { get; set; }

        public bool LegacyMode { get; set; }

        public bool ShouldFixId { get; set; } = true;
    }
}
