// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmXrefInlineRule : IMarkdownRule
    {
        private static readonly Regex _xrefRegex = new Regex(@"^@(?:(['""])(\s*?\S+?[\s\S]*?)\1|(?:([^'""][\S]*?))(?=[.,;:!?~\s]{2,}|[.,;:!?~]*$|\s))", RegexOptions.Compiled);
        public string Name => "XREF";

        /// <summary>
        /// XREF regex:
        ///     1. If content after `@` is wrapped by `'` or `"`,  it contains any character including white space
        ///     2. If content after `@` is not wrapped by `'` or `"`, it ends when
        ///         a. line ends
        ///         b. meets whitespaces
        ///         c. line ends with `.`, `,`, `;`, `:`, `!`, `?` and `~`
        ///         d. meets 2 times or more `.`, `,`, `;`, `:`, `!`, `?` and `~`
        /// </summary>
        public virtual Regex Xref => _xrefRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Xref.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // @String=>cap[3]=String, @'string'=>cap[2]=string
            // For cross-reference, add ~/ prefix
            var content = string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
            return new DfmXrefInlineToken(this, engine.Context, content, null, null, match.Value);
        }
    }
}
