// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    /// <summary>
    /// XREF regex:
    ///     1. If content after `@` is wrapped by `'` or `"`,  it contains any character including white space
    ///     2. If content after `@` is not wrapped by `'` or `"`,
    ///        It must start with word character `a-z` or `A-Z`
    ///        It ends when
    ///         a. line ends
    ///         b. meets whitespaces
    ///         c. line ends with `.`, `,`, `;`, `:`, `!`, `?` and `~`
    ///         d. meets 2 times or more `.`, `,`, `;`, `:`, `!`, `?` and `~`
    /// </summary>
    public class DfmXrefShortcutInlineRule : IMarkdownRule
    {
        private static readonly Regex XrefShortcutRegexWithQuote = new Regex(@"^@(?:(['""])(\s*?\S+?[\s\S]*?)\1)", RegexOptions.Compiled);
        private static readonly Regex XrefShortcutRegex = new Regex(@"^@((?:([a-z]+?[\S]*?))(?=[.,;:!?~\s]{2,}|[.,;:!?~]*$|\s))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Name => "XrefShortcut";

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = XrefShortcutRegexWithQuote.Match(source);
            if (match.Length == 0)
            {
                match = XrefShortcutRegex.Match(source);
                if (match.Length == 0)
                {
                    return null;
                }
            }

            source = source.Substring(match.Length);

            // @String=>cap[2]=String, @'string'=>cap[2]=string
            // For cross-reference, add ~/ prefix
            var content = match.Groups[2].Value;
            return new DfmXrefInlineToken(this, engine.Context, content, null, null, false, match.Value);
        }
    }
}
