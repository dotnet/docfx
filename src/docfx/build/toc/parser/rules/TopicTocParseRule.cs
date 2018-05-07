// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// 1. # [tocTitle](../specs/design/tableofcontents.md?branch=master#row=23)
    /// 2. # [tocTitle](~/doc/specs/design/tableofcontents.md "TOC design spec")
    /// 3. # [tocTitle](~/specs/design/)
    /// 4. # [tocTitle](~/specs/design/toc.md)
    /// </summary>
    internal sealed class TopicTocParseRule : TableOfContentsParseRule
    {
        public static readonly Regex TocRegex =
            new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)(\)| ""(?<displayText>.*)""\))(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => TocRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match)
        {
            var tocLink = match.Groups["tocLink"].Value;
            var tocTitle = match.Groups["tocTitle"].Value;
            var headerLevel = match.Groups["headerLevel"].Value.Length;
            string tocDisplayTitle = null;

            var displayGrp = match.Groups["displayText"];

            if (displayGrp.Success)
            {
                tocDisplayTitle = displayGrp.Value;
            }

            return ApplyCore(state, headerLevel, tocTitle, tocLink, tocDisplayTitle);
        }
    }
}
