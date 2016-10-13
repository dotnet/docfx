// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmEmailInlineRule : IMarkdownRule
    {
        private static readonly Regex _emailRegex = new Regex(@"^\s*[\w._%+-]*[\w_%+-]@[\w.-]+\.[\w]{2,}\b", RegexOptions.Compiled, TimeSpan.FromSeconds(10));
        public string Name => "DfmEmail";

        public virtual Regex Xref => _emailRegex;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Xref.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownTextToken(this, parser.Context, match.Groups[0].Value, sourceInfo);
        }
    }
}
