// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public class DfmTagNameResolveResult
    {
        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public HashSet<int> ExcludesLines { get; set; }

        public bool IsSuccessful { get; set; }

        public string ErrorMessage { get; set; }
    }
}