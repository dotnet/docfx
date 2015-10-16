// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEmInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Em";

        public virtual Regex Em => Regexes.Inline.Em;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Em.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownEmInlineToken(this, match.NotEmpty(2, 1));
        }
    }
}
