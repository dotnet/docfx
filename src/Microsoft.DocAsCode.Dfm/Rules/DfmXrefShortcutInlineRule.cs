// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Immutable;
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
        private const string ContinuableCharacters = ".,;:!?~";
        private const string StopCharacters = @"\s\""\'<>";

        public static readonly string XrefShortcutRegexWithQuoteString = @"@(?:(['""])(?<uid>\s*?\S+?[\s\S]*?)\1)";
        public static readonly string XrefShortcutRegexString = $@"@(?<uid>[a-zA-Z](?:[{ContinuableCharacters}]?[^{StopCharacters}{ContinuableCharacters}])*)";

        private static readonly Regex XrefShortcutRegexWithQuote = new Regex("^" + XrefShortcutRegexWithQuoteString, RegexOptions.Compiled, TimeSpan.FromSeconds(10));
        private static readonly Regex XrefShortcutRegex = new Regex("^" + XrefShortcutRegexString, RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        public string Name => "DfmXrefShortcut";

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (MarkdownInlineContext.GetIsInLink(parser.Context))
            {
                return null;
            }
            var match = XrefShortcutRegexWithQuote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                match = XrefShortcutRegex.Match(context.CurrentMarkdown);
                if (match.Length == 0)
                {
                    return null;
                }
            }

            var sourceInfo = context.Consume(match.Length);

            // @String=>cap[2]=String, @'string'=>cap[2]=string
            // For cross-reference, add ~/ prefix
            var content = match.Groups["uid"].Value;
            return new DfmXrefInlineToken(this, parser.Context, content, ImmutableArray<IMarkdownToken>.Empty, null, false, sourceInfo);
        }
    }
}
