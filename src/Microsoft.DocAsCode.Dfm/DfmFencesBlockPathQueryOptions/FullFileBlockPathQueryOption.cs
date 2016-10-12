﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public class FullFileBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        public override bool ValidateAndPrepare(string[] lines, DfmFencesToken token)
        {
            return true;
        }

        public override IEnumerable<string> GetQueryLines(string[] lines)
        {
            foreach (var line in lines)
            {
                yield return line;
            }
        }
    }
}
