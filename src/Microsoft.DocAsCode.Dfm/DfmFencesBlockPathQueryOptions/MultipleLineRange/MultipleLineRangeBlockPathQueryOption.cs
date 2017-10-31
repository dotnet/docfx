﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;

    public class MultipleLineRangeBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        public List<Tuple<int?, int?>> LinePairs { get; set; } = new List<Tuple<int?, int?>>();

        public override bool ValidateAndPrepare(string[] lines, DfmFencesToken token)
        {
            foreach (var pair in LinePairs)
            {
                if (!CheckLineRange(lines.Length, pair.Item1, pair.Item2))
                {
                    return false;
                }
            }

            return true;
        }

        public override IEnumerable<string> GetQueryLines(string[] lines)
        {
            foreach (var pair in LinePairs)
            {
                int startLine = pair.Item1 ?? 1;
                int endLine = pair.Item2 ?? lines.Length;

                for (int i = startLine; i <= Math.Min(endLine, lines.Length); i++)
                {
                    yield return lines[i - 1];
                }
            }
        }
    }
}
