﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmNoteBlockSplitToken : SplitToken
    {
        public DfmNoteBlockSplitToken(IMarkdownToken token) : base(token) { }
    }
}
