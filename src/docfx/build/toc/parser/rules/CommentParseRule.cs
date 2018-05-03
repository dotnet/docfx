// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// comment like <!-- comment text -->
    /// </summary>
    internal sealed class CommentParseRule : TableOfContentsParseRule
    {
        public static readonly Regex CommentRegex =
            new Regex(@"^\s*<!--[\s\S]*?-->\s*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => CommentRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match) => state;
    }
}
