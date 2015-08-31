// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// GFM + Line Breaks Inline Grammar
    /// </summary>
    public class BreaksInlineRules : GfmInlineRules
    {

        public override Regex Br { get { return Regexes.Inline.Breaks.Br; } }

        public override Regex Text { get { return Regexes.Inline.Breaks.Text; } }

    }
}
