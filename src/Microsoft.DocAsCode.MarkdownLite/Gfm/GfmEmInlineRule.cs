// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmEmInlineRule : MarkdownEmInlineRule
    {
        public override string Name => "Inline.Gfm.Em";

        public override Regex Em => Regexes.Inline.Gfm.Em;
    }
}
