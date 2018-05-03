// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal abstract class TableOfContentsParseRule
    {
        public abstract Match Match(string text);

        public abstract TableOfContentsParseState Apply(TableOfContentsParseState state, Match match);

        protected TableOfContentsParseState ApplyCore(TableOfContentsParseState state, int level, string text, string href, string displayText = null)
        {
            if (level > state.Level + 1)
            {
                return new ErrorState(state, level, $"Skip level is not allowed. Toc content: {text}");
            }

            // If current node is another node in higher or same level
            for (int i = state.Level; i >= level; --i)
            {
                state.Parents.Pop();
            }

            var item = new TableOfContentsItem
            {
                Name = text,
                DisplayName = displayText,
                Href = href,
            };
            if (state.Parents.Count > 0)
            {
                var parent = state.Parents.Peek();
                if (parent.Items == null)
                {
                    parent.Items = new List<TableOfContentsItem>();
                }
                parent.Items.Add(item);
            }
            else
            {
                state.Root.Add(item);
            }
            state.Parents.Push(item);

            if (state.Level == level)
            {
                return state;
            }
            return new NodeState(state, level);
        }
    }

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

    internal sealed class WhitespaceParseRule : TableOfContentsParseRule
    {
        public static readonly Regex WhitespaceRegex =
            new Regex(@"^\s*(\n|$)", RegexOptions.Compiled);

        public override Match Match(string text) => WhitespaceRegex.Match(text);

        public override TableOfContentsParseState Apply(TableOfContentsParseState state, Match match) => state;
    }
}
