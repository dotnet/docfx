// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal sealed class WhitespaceParseRule : TableOfContentsParseRule
    {
        public static readonly Regex WhitespaceRegex =
            new Regex(@"^\s*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => WhitespaceRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match) => state;
    }
}
