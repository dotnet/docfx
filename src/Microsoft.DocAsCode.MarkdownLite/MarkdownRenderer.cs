﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownRenderer
    {
        #region Block

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            // do nothing.
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownCodeBlockToken token, MarkdownBlockContext context)
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

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var result = (StringBuffer)"<h"
                + level
                + " id=\""
                + engine.Options.HeaderPrefix
                + token.Id
                + "\">";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</h";
            result += level;
            result += ">\n";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return engine.Options.XHtml ? "<hr/>\n" : "<hr>\n";
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = "<blockquote>\n";
            content += RenderTokens(engine, token.Tokens, context, true, token.Rule);
            return content + "</blockquote>\n";
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownListBlockToken token, MarkdownBlockContext context)
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

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownListItemBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = "<li>";
            content += RenderTokens(engine, token.Tokens, context, token.Loose, token.Rule);
            return content + "</li>\n";
        }

        protected StringBuffer RenderTokens(MarkdownEngine engine, ImmutableArray<IMarkdownToken> tokens, MarkdownBlockContext context, bool wrapParagraph = false, IMarkdownRule rule = null)
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
                    content += RenderTextInTokens(engine, context, wrapParagraph, rule, textContent);
                    textContent = StringBuffer.Empty;
                }
                content += engine.Render(t);
            }
            if (textContent != StringBuffer.Empty)
            {
                content += RenderTextInTokens(engine, context, wrapParagraph, rule, textContent);
            }
            return content;
        }

        private StringBuffer RenderTextInTokens(MarkdownEngine engine, MarkdownBlockContext context, bool wrapParagraph, IMarkdownRule rule, StringBuffer textContent)
        {
            if (wrapParagraph)
            {
                return Render(engine, new MarkdownParagraphBlockToken(rule, context, engine.TokenizeInline(textContent)), context);
            }
            else
            {
                return ApplyInline(engine, textContent, context);
            }
        }

        protected virtual StringBuffer ApplyInline(MarkdownEngine engine, StringBuffer content, MarkdownBlockContext context)
        {
            if (content == StringBuffer.Empty)
            {
                return StringBuffer.Empty;
            }
            var c = engine.SwitchContext(context.InlineContext);
            var result = engine.Mark(content.ToString());
            engine.SwitchContext(c);
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            foreach (var item in token.InlineTokens)
            {
                result += engine.Render(item);
            }
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownTextToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<p>";
            result += token.Content;
            result += "</p>\n";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer result = "<table>\n<thead>\n";
            // header
            result += "<tr>\n";
            var cell = StringBuffer.Empty;
            var c = engine.SwitchContext(context.InlineContext);
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
                result += engine.Mark(token.Header[i]);
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
                    result += engine.Mark(row[j]);
                    result += "</td>\n";
                }
                result += "</tr>\n";
            }
            engine.SwitchContext(c);
            return result + "</tbody>\n" + "</table>\n";
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownLinkInlineToken token, MarkdownInlineContext context)
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

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<img src=\"" + token.Href + "\" alt=\"" + token.Text + "\"";
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + token.Title + "\"";
            }

            result += engine.Options.XHtml ? "/>" : ">";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<strong>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</strong>";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<em>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</em>";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return (StringBuffer)"<code>" + StringHelper.Escape(token.Content, true) + "</code>";
        }

        public virtual StringBuffer Render(MarkdownEngine engine, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var result = (StringBuffer)"<del>";
            foreach (var item in token.Content)
            {
                result += engine.Render(item);
            }
            result += "</del>";
            return result;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownTagInlineToken token, MarkdownInlineContext context)
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

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return engine.Options.XHtml ? "<br/>" : "<br>";
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return token.Content;
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, MarkdownRawToken token, IMarkdownContext context)
        {
            return token.RawMarkdown;
        }

        #endregion
    }
}
