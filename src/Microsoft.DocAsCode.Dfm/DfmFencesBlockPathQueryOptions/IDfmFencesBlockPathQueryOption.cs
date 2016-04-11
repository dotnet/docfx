// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public interface IDfmFencesBlockPathQueryOption
    {
        string ErrorMessage { get; }

        bool ValidateAndPrepare(string[] lines, DfmFencesBlockToken token);

        IEnumerable<string> GetQueryLines(string[] lines);
    }
}
