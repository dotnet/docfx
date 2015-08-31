// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// GFM Inline Grammar
    /// </summary>
    public class GfmInlineRules : InlineRules
    {

        public override Regex Escape { get { return Regexes.Inline.Gfm.Escape; } }

        public override Regex Url { get { return Regexes.Inline.Gfm.Url; } }

        public override Regex Del { get { return Regexes.Inline.Gfm.Del; } }

        public override Regex Text { get { return Regexes.Inline.Gfm.Text; } }

    }
}
