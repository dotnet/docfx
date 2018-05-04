// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// # [externalLinkTitle] (https://github.com/dotnet/docfx)
    /// </summary>
    internal sealed class ExternalLinkTocParseRule : TableOfContentsParseRule
    {
        public static readonly Regex TocRegex =
            new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+?)\]\((?<tocLink>(http[s]?://).*?)\)(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => TocRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match)
        {
            return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, match.Groups["tocLink"].Value);
        }
    }
}
