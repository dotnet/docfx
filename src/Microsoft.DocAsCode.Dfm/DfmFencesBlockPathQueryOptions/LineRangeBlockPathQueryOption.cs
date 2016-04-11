﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;

    class LineRangeBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        public int? StartLine { get; set; }

        public int? EndLine { get; set; }

        public override bool ValidateAndPrepare(string[] lines, DfmFencesBlockToken token)
        {
            if (!CheckLineRange(lines.Length, StartLine, EndLine))
            {
                return false;
            }

            return true;
        }

        public override IEnumerable<string> GetQueryLines(string[] lines)
        {
            int startLine = StartLine ?? 1;
            int endLine = EndLine ?? lines.Length;

            for (int i = startLine; i <= Math.Min(endLine, lines.Length); i++)
            {
                yield return lines[i - 1];
            }
        }
    }
}
