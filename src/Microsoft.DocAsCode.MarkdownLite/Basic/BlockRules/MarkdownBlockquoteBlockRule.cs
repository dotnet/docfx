// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownBlockquoteBlockRule : IMarkdownRule
    {
        public virtual string Name => "Blockquote";

        public virtual Regex Blockquote => Regexes.Block.Blockquote;

        public virtual Regex LeadingBlockquote => Regexes.Lexers.LeadingBlockquote;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Blockquote.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            var capStr = LeadingBlockquote.Replace(match.Value, string.Empty);
            return new MarkdownBlockquoteBlockToken(this, engine.Context, engine.Tokenize(capStr), match.Value);
        }
    }
}
