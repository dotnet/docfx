// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmBlockRules : BlockRules
    {

        public override Regex Fences { get { return Regexes.Block.Gfm.Fences; } }

        public override Regex Paragraph { get { return Regexes.Block.Gfm.Paragraph; } }

        public override Regex Heading { get { return Regexes.Block.Gfm.Heading; } }

    }
}
