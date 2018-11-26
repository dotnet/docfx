// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Linq;

    public static class DfmCodeExtractorHelper
    {
        public static readonly List<char> AllowedIndentCharacters = new List<char> { ' ', '\t' };

        public static int GetIndentLength(string s) => s.TakeWhile(c => AllowedIndentCharacters.Contains(c)).Count();
    }
}