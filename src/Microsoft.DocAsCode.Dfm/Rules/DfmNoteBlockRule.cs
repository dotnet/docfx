// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmNoteBlockRule : IMarkdownRule
    {
        private static readonly Regex _dfmNoteRegex = new Regex(@"^(?<rawmarkdown> *\[\!(?<notetype>(NOTE|WARNING|TIP|IMPORTANT|CAUTION))\] *\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        public virtual string Name => "DfmNote";

        public virtual Regex DfmNoteRegex => _dfmNoteRegex;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
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
