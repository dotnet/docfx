// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;

    public class MarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            return token.RawMarkdown;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;
            foreach (var t in token.InlineTokens.Tokens)
            {
                content += render.Render(t);
            }
            return content + "\n\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            const string BlockQuoteStartString = "> ";
            const string BlockQuoteJoinString = "\n" + BlockQuoteStartString;

            var content = StringBuffer.Empty;
            foreach (var t in token.Tokens)
            {
                content += render.Render(t);
            }
            var contents = content.ToString().Split('\n');
            content = StringBuffer.Empty;
            foreach (var item in contents)
            {
                if (content == StringBuffer.Empty)
                {
                    content += BlockQuoteStartString;
                    content += item;
                }
                else
                {
                    content += BlockQuoteJoinString;
                    content += item;
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            const string ListStartString = "* ";
            var content = StringBuffer.Empty;

            if (token.Ordered)
            {
                foreach (var t in token.Tokens)
                {
                    var listItemToken = t as MarkdownListItemBlockToken;
                    if (listItemToken == null)
                    {
                        throw new Exception($"token {t.GetType()} is not MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{t.RawMarkdown}");
                    }
                    content += ListStartString;
                    content += render.Render(t);
                }
            }
            else
            {
                for (int i = 1; i < token.Tokens.Length; ++i)
                {
                    var listItemToken = token.Tokens[i] as MarkdownListItemBlockToken;

                    if (listItemToken == null)
                    {
                        throw new Exception($"token {token.Tokens[i].GetType()} is not MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{token.Tokens[i].RawMarkdown}");
                    }

                    content += i.ToString();
                    content += ". ";
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownListItemBlockToken token, MarkdownBlockContext context)
        {
            // TODO: Add corresponding white space before the result
            var content = StringBuffer.Empty;
            foreach (var t in token.Tokens)
            {
                content += render.Render(t);
            }
            return content;
        }
    }
}
