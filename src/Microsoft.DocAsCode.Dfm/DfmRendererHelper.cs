// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.MarkdownLite;

    internal class DfmRendererHelper
    {
        public static string GetRenderedFencesBlockString(DfmFencesBlockToken token, Options options, string errorMessage, string[] codeLines = null)
        {
            string renderedErrorMessage = string.Empty;
            string renderedCodeLines = string.Empty;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                renderedErrorMessage = $@"<!-- {StringHelper.HtmlEncode(errorMessage)} -->\n";
            }

            if (codeLines != null)
            {
                var lang = string.IsNullOrEmpty(token.Lang) ? null : $" class=\"{options.LangPrefix}{token.Lang}\"";
                var name = string.IsNullOrEmpty(token.Name) ? null : $" name=\"{StringHelper.HtmlEncode(token.Name)}\"";
                var title = string.IsNullOrEmpty(token.Title) ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";

                var nonBlank  = codeLines.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                var trimChars = 0;

                while (nonBlank.Count > 0)
                {
                    // thisChar is whatever the first string has in this position
                    var thisChar = nonBlank[0][trimChars];

                    // if we're not dealing with a space or a tab, then we can be done now
                    if (thisChar != ' ' && thisChar != '\t')
                        break;

                    // if one of the lines doesn't have thisChar at the current position, then we can be done
                    if (!nonBlank.All(s => s[trimChars] == thisChar))
                        break;

                    // otherwise, advance our position
                    trimChars++;
                }

                renderedCodeLines = $"<pre><code{lang}{name}{title}>{StringHelper.HtmlEncode(string.Join("\n", codeLines.Select(l => l.Length >= trimChars ? l.Substring(trimChars) : l)))}\n</code></pre>";
            }

            return $"{renderedErrorMessage}{renderedCodeLines}";
        }

        public class SplitToken
        {
            public IMarkdownToken Token { get; set; }

            public List<IMarkdownToken> InnerTokens { get; set; }

            public SplitToken(IMarkdownToken token)
            {
                Token = token;
                InnerTokens = new List<IMarkdownToken>();
            }
        }

        public static List<SplitToken> SplitBlockquoteTokens(ImmutableArray<IMarkdownToken> tokens)
        {
            var splitTokens = new List<SplitToken>();
            SplitToken splitToken = null;
            foreach (var token in tokens)
            {
                if (token is DfmSectionBlockToken || token is DfmNoteBlockToken)
                {
                    splitToken = new SplitToken(token);
                    splitTokens.Add(splitToken);
                }
                else
                {
                    if (splitToken != null)
                    {
                        splitToken.InnerTokens.Add(token);
                        continue;
                    }
                    splitToken = new SplitToken(token);
                    splitToken.InnerTokens.Add(token);
                    splitTokens.Add(splitToken);
                }
            }
            return splitTokens;
        }
    }
}
