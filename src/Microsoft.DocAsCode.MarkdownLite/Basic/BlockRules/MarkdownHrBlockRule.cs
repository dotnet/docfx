// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHrBlockRule : IMarkdownRule
    {
        public string Name => "Hr";

        public virtual Regex Hr => Regexes.Block.Hr;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Hr.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownHrBlockToken(this, engine.Context);
        }
    }
}
