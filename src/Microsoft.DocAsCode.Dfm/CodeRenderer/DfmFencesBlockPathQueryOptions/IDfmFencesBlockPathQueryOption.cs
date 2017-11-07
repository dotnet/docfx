// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;

    public interface IDfmFencesBlockPathQueryOption
    {
        string HighlightLines { get; set; }

        int? DedentLength { get; set; }

        string ErrorMessage { get; }

        bool ValidateHighlightLinesAndDedentLength(int totalLines);

        IEnumerable<string> GetQueryLines(string[] lines, DfmFencesToken token);
    }
}
