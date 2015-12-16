// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownDefBlockRule : IMarkdownRule
    {
        public string Name => "Def";

        public virtual Regex Def => Regexes.Block.Def;

        public virtual IMarkdownToken TryMatch(MarkdownParser engine, ref string source)
        {
            if (!(bool)engine.Context.Variables[MarkdownBlockContext.IsTop])
            {
                return null;
            }
            var match = Def.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            engine.Links[match.Groups[1].Value.ToLower()] = new LinkObj
            {
                Href = match.Groups[2].Value,
                Title = match.Groups[3].Value
            };
            return new MarkdownIgnoreToken(this, engine.Context);
        }
    }
}
