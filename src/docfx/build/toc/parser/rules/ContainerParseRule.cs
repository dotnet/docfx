// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// # containerTitle
    /// </summary>
    internal sealed class ContainerParseRule : TableOfContentsParseRule
    {
        public static readonly Regex ContainerRegex =
            new Regex(@"^(?<headerLevel>#+)(( |\t)*)(?<tocTitle>.+?)(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => ContainerRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match)
        {
            return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, null);
        }
    }
}
