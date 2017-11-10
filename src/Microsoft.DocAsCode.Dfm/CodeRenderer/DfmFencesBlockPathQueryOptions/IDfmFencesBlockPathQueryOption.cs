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

        [Obsolete("merged into GetQueryLines(string[], DfmFencesToken)", true)]
        bool ValidateAndPrepare(string[] lines, DfmFencesToken token);

        [Obsolete("merged into GetQueryLines(string[], DfmFencesToken)", true)]
        bool ValidateHighlightLinesAndDedentLength(int totalLines);

        [Obsolete("merged into GetQueryLines(string[], DfmFencesToken)", true)]
        IEnumerable<string> GetQueryLines(string[] lines);

        IEnumerable<string> GetQueryLines(string[] lines, DfmFencesToken token);
    }
}
