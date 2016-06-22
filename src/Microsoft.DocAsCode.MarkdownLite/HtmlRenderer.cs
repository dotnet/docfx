// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The html renderer for gfm.
    /// </summary>
    public class HtmlRenderer
    {
        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            // do nothing.
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            bool escaped = false;
            string code = token.Code;
            if (renderer.Options.Highlight != null)
            {
                var highlightCode = renderer.Options.Highlight(code, token.Lang);
                if (highlightCode != null && highlightCode != code)
                {
                    escaped = true;
                    code = highlightCode;
                }
            }

            if (string.IsNullOrEmpty(token.Lang))
            {
                return (StringBuffer)"<pre><code>" + (escaped ? code : StringHelper.Escape(code, true)) + "\n</code></pre>";
            }

            return "<pre><code class=\""
                + renderer.Options.LangPrefix
                + StringHelper.Escape(token.Lang, true)
                + "\">"
                + (escaped ? code : StringHelper.Escape(code, true))
                + "\n</code></pre>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var result = (StringBuffer)"<h"
                + level
                + " id=\""
                + renderer.Options.HeaderPrefix
                + token.Id
                + "\">";
            foreach (var item in token.Content.Tokens)
            {
                result += renderer.Render(item);
            }
            result += "</h";
            result += level;
            result += ">\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return renderer.Options.XHtml ? "<hr/>\n" : "<hr>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = "<blockquote>\n";
            foreach (var item in token.Tokens)
            {
                content += renderer.Render(item);
            }
            return content + "</blockquote>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            StringBuffer content = "<";
            content += type;
            content += ">\n";
            foreach (var t in token.Tokens)
            {
                content += Render(renderer, (MarkdownListItemBlockToken)t);
            }
            return content + "</" + type + ">\n";
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            StringBuffer content = "<li>";
            foreach (var item in token.Tokens)
            {
                content += renderer.Render(item);
            }
            return content + "</li>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                result += renderer.Render(item);
            }
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            foreach (var item in token.InlineTokens.Tokens)
            {
                result += renderer.Render(item);
            }
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            result += token.Content;
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<table>\n<thead>\n";
            // header
            result += "<tr>\n";
            var cell = StringBuffer.Empty;
            for (int i = 0; i < token.Header.Length; i++)
            {
                if (i < token.Align.Length && token.Align[i] != Align.NotSpec)
                {
                    result += "<th style=\"text-align:";
                    result += token.Align[i].ToString().ToLower();
                    result += "\">";
                }
                else
                {
                    result += "<th>";
                }
                foreach (var item in token.Header[i].Tokens)
                {
                    result += renderer.Render(item);
                }
                result += "</th>\n";
            }
            result += "</tr>\n";
            result += "</thead>\n";
            result += "<tbody>\n";
            // body
            for (int i = 0; i < token.Cells.Length; i++)
            {
                var row = token.Cells[i];
                result += "<tr>\n";
                for (int j = 0; j < row.Length; j++)
                {
                    if (j < token.Align.Length && token.Align[j] != Align.NotSpec)
                    {
                        result += "<td style=\"text-align:";
                        result += token.Align[j].ToString().ToLower();
                        result += "\">";
                    }
                    else
                    {
                        result += "<td>";
                    }
                    foreach (var item in row[j].Tokens)
                    {
                        result += renderer.Render(item);
                    }
                    result += "</td>\n";
                }
                result += "</tr>\n";
            }
            return result + "</tbody>\n" + "</table>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                result += renderer.Render(item);
            }
            return result;
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            if (renderer.Options.Sanitize)
            {
                string prot = null;

                try
                {
                    prot = Regex.Replace(StringHelper.DecodeURIComponent(StringHelper.Unescape(token.Href)), @"[^\w:]", string.Empty).ToLower();
                }
                catch (Exception)
                {
                    return string.Empty;
                }

                if (prot.IndexOf("javascript:") == 0 || prot.IndexOf("vbscript:") == 0)
                {
                    return string.Empty;
                }
            }

            var result = (StringBuffer)"<a href=\"" + token.Href + "\"";
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + StringHelper.HtmlEncode(token.Title) + "\"";
            }
            result += ">";

            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }

            result += "</a>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<img src=\"" + token.Href + "\" alt=\"" + StringHelper.HtmlEncode(token.Text) + "\"";
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + StringHelper.HtmlEncode(token.Title) + "\"";
            }

            result += renderer.Options.XHtml ? "/>" : ">";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<strong>";
            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }
            result += "</strong>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<em>";
            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }
            result += "</em>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return (StringBuffer)"<code>" + StringHelper.Escape(token.Content, true) + "</code>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<del>";
            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }
            result += "</del>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            if (renderer.Options.Sanitize)
            {
                if (renderer.Options.Sanitizer != null)
                {
                    return renderer.Options.Sanitizer(token.SourceInfo.Markdown);
                }
                return StringHelper.Escape(token.SourceInfo.Markdown);
            }
            return token.SourceInfo.Markdown;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return renderer.Options.XHtml ? "<br/>" : "<br>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return token.SourceInfo.Markdown;
        }

        #endregion
    }
}
