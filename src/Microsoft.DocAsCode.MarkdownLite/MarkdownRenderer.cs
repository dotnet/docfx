﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Linq;

    public class MarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            return token.SourceInfo.Markdown;
        }

        #region Inline Token

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            content += "![";
            content += token.Text;
            content += "](";
            content += Regexes.Helper.MarkdownUnescape.Replace(StringHelper.Unescape(token.Href), m => "\\" + m.Value);
            if (!string.IsNullOrEmpty(token.Title))
            {
                content += " \"";
                content += Regexes.Helper.MarkdownUnescape.Replace(StringHelper.Unescape(token.Title), m => "\\" + m.Value);
                content += "\"";
            }
            content += ")";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            content += "[";
            foreach (var t in token.Content)
            {
                content += render.Render(t);
            }
            content += "](";
            content += Regexes.Helper.MarkdownUnescape.Replace(StringHelper.Unescape(token.Href), m => "\\" + m.Value);
            if (!string.IsNullOrEmpty(token.Title))
            {
                content += " \"";
                content += Regexes.Helper.MarkdownUnescape.Replace(StringHelper.Unescape(token.Title), m => "\\" + m.Value);
                content += "\"";
            }
            content += ")";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            content += "~~";
            foreach (var t in token.Content)
            {
                content += render.Render(t);
            }
            content += "~~";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            content += "*";
            foreach (var t in token.Content)
            {
                content += render.Render(t);
            }
            content += "*";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            content += "**";
            foreach (var t in token.Content)
            {
                content += render.Render(t);
            }
            content += "**";
            return content;
        }

        #endregion

        #region Block Token

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            foreach (var t in token.Content.Tokens)
            {
                content += render.Render(t);
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return "- - -\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            for (int i = 0; i < token.Depth; ++i)
            {
                content += "#";
            }
            content += " ";

            foreach (var t in token.Content.Tokens)
            {
                content += render.Render(t);
            }
            content += "\n";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            foreach (var t in token.Content.Tokens)
            {
                content += render.Render(t);
            }
            return content + "\n";
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

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;

            // Generate header line
            content += "|";
            foreach (var header in token.Header)
            {
                content += " ";
                foreach (var t in header.Content.Tokens)
                {
                    content += render.Render(t);
                }
                content += " |";
            }
            content += "\n";

            // Generate align line
            content += "|";
            foreach (var align in token.Align)
            {
                switch (align)
                {
                    case Align.NotSpec:
                        content += " --- ";
                        break;
                    case Align.Left:
                        content += ":--- ";
                        break;
                    case Align.Right:
                        content += " ---:";
                        break;
                    case Align.Center:
                        content += ":---:";
                        break;
                    default:
                        throw new NotSupportedException($"align:{align} doesn't support in GFM table");
                }
                content += "|";
            }
            content += "\n";

            // Generate content lines
            foreach (var row in token.Cells)
            {
                content += "| ";
                foreach (var column in row)
                {
                    foreach (var t in column.Content.Tokens)
                    {
                        content += render.Render(t);
                    }
                    content += " |";
                }
                content += "\n";
            }

            return content += "\n";
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
            return content + "\n\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;

            if (!token.Ordered)
            {
                const string ListStartString = "* ";
                foreach (var t in token.Tokens)
                {
                    var listItemToken = t as MarkdownListItemBlockToken;
                    if (listItemToken == null)
                    {
                        throw new Exception($"token {t.GetType()} is not unordered MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{t.SourceInfo.Markdown}");
                    }
                    content += ListStartString;
                    content += Render(render, listItemToken, "  ");
                }
            }
            else
            {
                for (int i = 0; i < token.Tokens.Length; ++i)
                {
                    var listItemToken = token.Tokens[i] as MarkdownListItemBlockToken;

                    if (listItemToken == null)
                    {
                        throw new Exception($"token {token.Tokens[i].GetType()} is not ordered MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{token.Tokens[i].SourceInfo.Markdown}");
                    }

                    content += $"{i + 1}. ";
                    string indent = new string(' ', (i + 1).ToString().Length + 2);
                    content += Render(render, listItemToken, indent);
                }
            }
            return content + "\n";
        }

        protected virtual StringBuffer Render(IMarkdownRenderer render, MarkdownListItemBlockToken token, string indent)
        {
            var content = StringBuffer.Empty;
            if (token.Tokens.Length > 0)
            {
                var tokenRenderContent = StringBuffer.Empty;
                foreach (var t in token.Tokens)
                {
                    tokenRenderContent += render.Render(t);
                }

                var lines = tokenRenderContent.ToString().TrimEnd('\n').Split('\n');
                content += lines[0];
                content += "\n";
                foreach (var line in lines.Skip(1))
                {
                    content += indent;
                    content += line;
                    content += "\n";
                }
            }
            return content;
        }

        #endregion
    }
}
