// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class DfmNoteBlockRule : IMarkdownRule
    {
        private static readonly Matcher _NoteMatcher =
            Matcher.WhiteSpacesOrEmpty +
            "[!" +
            (
                Matcher.CaseInsensitiveString("note") |
                Matcher.CaseInsensitiveString("warning") |
                Matcher.CaseInsensitiveString("tip") |
                Matcher.CaseInsensitiveString("important") |
                Matcher.CaseInsensitiveString("caution") | 
                Matcher.CaseInsensitiveString("next")
            ).ToGroup("notetype") +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            Matcher.NewLine.RepeatAtLeast(0);

        private static readonly Regex _dfmNoteRegex = new Regex(@"^(?<rawmarkdown> *\[\!(?<notetype>(NOTE|WARNING|TIP|IMPORTANT|CAUTION|NEXT))\] *\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        public virtual string Name => "DfmNote";

        [Obsolete]
        public virtual Regex DfmNoteRegex => _dfmNoteRegex;

        public virtual Matcher NoteMatcher => _NoteMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            if (DfmNoteRegex != _dfmNoteRegex || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(NoteMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new DfmNoteBlockToken(this, parser.Context, match["notetype"].GetValue(), sourceInfo.Markdown, sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = DfmNoteRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Groups["rawmarkdown"].Length);
            return new DfmNoteBlockToken(this, parser.Context, match.Groups["notetype"].Value, match.Groups["rawmarkdown"].Value, sourceInfo);
        }
    }
}
