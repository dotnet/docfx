// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    /// <summary>
    /// Fake rule for Dfm Tab group.
    /// </summary>
    public class DfmTabGroupBlockRule : IMarkdownRule
    {
        public static readonly DfmTabGroupBlockRule Instance = new DfmTabGroupBlockRule();

        public string Name => "DfmTabGroup";

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context) =>
            throw new NotSupportedException();
    }
}
