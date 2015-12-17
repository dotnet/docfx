// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmDelInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Del";

        public virtual Regex Del => Regexes.Inline.Gfm.Del;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Del.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new GfmDelInlineToken(this, engine.Context, engine.Tokenize(match.Groups[1].Value));
        }
    }
}
