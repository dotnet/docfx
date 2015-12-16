// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeBlockRule : IMarkdownRule
    {
        public string Name => "Code";

        public virtual Regex Code => Regexes.Block.Code;

        public virtual IMarkdownToken TryMatch(MarkdownParser engine, ref string source)
        {
            var match = Regexes.Block.Code.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(match.Value, string.Empty);
            if (engine.Options.Pedantic)
            {
                return new MarkdownCodeBlockToken(this, engine.Context, capStr);
            }
            else
            {
                return new MarkdownCodeBlockToken(this, engine.Context, Regexes.Lexers.TailingEmptyLines.Replace(capStr, string.Empty));
            }
        }
    }
}
