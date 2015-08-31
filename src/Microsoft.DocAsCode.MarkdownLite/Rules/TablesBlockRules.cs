// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// GFM + Tables Block Grammar
    /// </summary>
    public class TablesBlockRules : GfmBlockRules
    {

        public override Regex NpTable { get { return Regexes.Block.Tables.NpTable; } }

        public override Regex Table { get { return Regexes.Block.Tables.Table; } }

    }
}
