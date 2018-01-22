// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Markdig.Helpers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    internal class Tag
    {
        private static readonly Regex OpeningTag = new Regex(@"\<(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>", RegexOptions.Compiled);
        private static readonly Regex ClosingTag = new Regex(@"\</(\w+)((?:""[^""]*""|'[^']*'|[^'"">])*?)\>", RegexOptions.Compiled);

        public int Line { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public bool IsOpening { get; set; }

        public static IEnumerable<Tag> Convert(IMarkdownObject markdownObject)
        {
            if (markdownObject is HtmlBlock block)
            {
                return Convert(block);
            }

            if (markdownObject is HtmlInline inline)
            {
                return Convert(inline);
            }

            return null;
        }

        private static IEnumerable<Tag> Convert(HtmlBlock block)
        {
            var lines = block.Lines;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines.Lines[i];
                foreach (var tag in Convert(line) ?? Enumerable.Empty<Tag>())
                {
                    yield return tag;
                }
            }
        }

        private static IEnumerable<Tag> Convert(StringLine line)
        {
            var matches = OpeningTag.Matches(line.ToString());
            var isOpening = true;
            foreach (Match m in matches)
            {
                yield return new Tag
                {
                    Line = line.Line,
                    Content = line.ToString(),
                    Name = m.Groups[1].Value,
                    IsOpening = isOpening
                };
            }

            matches = ClosingTag.Matches(line.ToString());
            isOpening = false;
            foreach (Match m in matches)
            {
                yield return new Tag
                {
                    Line = line.Line,
                    Content = line.ToString(),
                    Name = m.Groups[1].Value,
                    IsOpening = isOpening
                };
            }
        }

        private static IEnumerable<Tag> Convert(HtmlInline inline)
        {
            var text = inline.Tag;
            var match = OpeningTag.Match(text);
            var isOpening = true;
            if (match.Length < 1)
            {
                match = ClosingTag.Match(text);
                isOpening = false;
            }

            yield return new Tag
            {
                Name = match.Groups[1].Value,
                Content = inline.Tag,
                IsOpening = isOpening,
                Line = inline.Line
            };
        }
    }
}
