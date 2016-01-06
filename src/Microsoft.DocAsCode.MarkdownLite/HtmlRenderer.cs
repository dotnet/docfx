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

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            // do nothing.
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            bool escaped = false;
            string code = token.Code;
            if (engine.Options.Highlight != null)
            {
                var highlightCode = engine.Options.Highlight(code, token.Lang);
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
                + engine.Options.LangPrefix
                + StringHelper.Escape(token.Lang, true)
                + "\">"
                + (escaped ? code : StringHelper.Escape(code, true))
                + "\n</code></pre>\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var result = (StringBuffer)"<h"
                + level
                + " id=\""
                + engine.Options.HeaderPrefix
                + token.Id
                + "\">";
            foreach (var item in token.Content.Tokens)
            {
                result += engine.Render(item);
            }
            result += "</h";
            result += level;
            result += ">\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return engine.Options.XHtml ? "<hr/>\n" : "<hr>\n";
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

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            StringBuffer content = "<";
            content += type;
            content += ">\n";
            foreach (var t in token.Tokens)
            {
                content += engine.Render(t);
            }
            return content + "</" + type + ">\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = "<li>";
            foreach (var item in token.Tokens)
            {
                content += renderer.Render(item);
            }
            return content + "</li>\n";
        }

        protected StringBuffer RenderTokens(IMarkdownRenderer engine, ImmutableArray<IMarkdownToken> tokens, MarkdownBlockContext context, bool wrapParagraph = false, IMarkdownRule rule = null)
        {
            var content = StringBuffer.Empty;
            var textContent = StringBuffer.Empty;
            foreach (var t in tokens)
            {
                var text = t as MarkdownTextToken;
                if (text != null)
                {
                    if (textContent != StringBuffer.Empty)
                    {
                        textContent += "\n";
                    }
                    textContent += text.Content;
                    continue;
                }
                if (!wrapParagraph && t is MarkdownNewLineBlockToken)
                {
                    continue;
                }
                if (textContent != StringBuffer.Empty)
                {
                    content += RenderTextInTokens(engine, context, wrapParagraph, rule, textContent, t.RawMarkdown);
                    textContent = StringBuffer.Empty;
                }
                content += engine.Render(t);
            }
            if (textContent != StringBuffer.Empty)
            {
                content += RenderTextInTokens(engine, context, wrapParagraph, rule, textContent, textContent);
            }
            return content;
        }

        private StringBuffer RenderTextInTokens(IMarkdownRenderer renderer, MarkdownBlockContext context, bool wrapParagraph, IMarkdownRule rule, StringBuffer textContent, string rawMarkdown)
        {
            if (wrapParagraph)
            {
                var parser = renderer.Engine.Parser;
                var c = parser.SwitchContext(context);
                var inlineContent = parser.TokenizeInline(textContent);
                parser.SwitchContext(c);
                return Render(renderer, new MarkdownParagraphBlockToken(rule, context, inlineContent, rawMarkdown), context);
            }
            else
            {
                return ApplyInline(renderer, textContent, context);
            }
        }

        protected virtual StringBuffer ApplyInline(IMarkdownRenderer engine, StringBuffer content, MarkdownBlockContext context)
        {
            if (content == StringBuffer.Empty)
            {
                return StringBuffer.Empty;
            }
            var result = engine.Engine.Mark(content.ToString(), context.GetInlineContext());
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                result += engine.Render(item);
            }
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            foreach (var item in token.InlineTokens.Tokens)
            {
                result += engine.Render(item);
            }
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownTextToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            result += token.Content;
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownTableBlockToken token, MarkdownBlockContext context)
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
                    result += engine.Render(item);
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
                        result += engine.Render(item);
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

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            if (engine.Options.Sanitize)
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
                result = result + " title=\"" + token.Title + "\"";
            }
            result += ">";

            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }

            result += "</a>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<img src=\"" + token.Href + "\" alt=\"" + token.Text + "\"";
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + token.Title + "\"";
            }

            result += engine.Options.XHtml ? "/>" : ">";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<strong>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</strong>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<em>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</em>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return (StringBuffer)"<code>" + StringHelper.Escape(token.Content, true) + "</code>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<del>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</del>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            if (engine.Options.Sanitize)
            {
                if (engine.Options.Sanitizer != null)
                {
                    return engine.Options.Sanitizer(token.RawMarkdown);
                }
                return StringHelper.Escape(token.RawMarkdown);
            }
            return token.RawMarkdown;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return engine.Options.XHtml ? "<br/>" : "<br>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, MarkdownRawToken token, IMarkdownContext context)
        {
            return token.RawMarkdown;
        }

        #endregion
    }
}
