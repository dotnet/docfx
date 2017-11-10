// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Linq;

    public class FullFileBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        public override IEnumerable<string> GetQueryLines(string[] lines, DfmFencesToken token)
        {
            return ProcessIncludedLines(lines.ToList(), token);
        }
    }
}
