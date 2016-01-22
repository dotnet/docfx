// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmNoteBlockRule : IMarkdownRule
    {
        public virtual string Name => "DfmNoteBlockRule";

        public virtual Regex DfmNoteRegex => new Regex(@"^(?<rawmarkdown> *\[\!(?<notetype>(NOTE|WARNING|TIP|IMPORTANT|CAUTION))\] *\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            if (!engine.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)engine.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            var match = DfmNoteRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Groups["rawmarkdown"].Length);
            return new DfmNoteBlockToken(this, engine.Context, match.Groups["notetype"].Value, match.Groups["rawmarkdown"].Value, match.Groups["rawmarkdown"].Value);
        }
    }
}
