// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    public class Renderer : IRenderer
    {

        #region Properties

        public Options Options { get; set; }

        #endregion

        #region Constructors

        public Renderer()
            : this(null)
        {
        }

        public Renderer(Options options)
        {
            Options = options ?? new Options();
        }

        #endregion

        #region Methods

        #region Block Level Renderer

        public virtual StringBuffer Code(string code, string lang, bool escaped)
        {
            if (Options.Highlight != null)
            {
                var highlightCode = Options.Highlight(code, lang);
                if (highlightCode != null && highlightCode != code)
                {
                    escaped = true;
                    code = highlightCode;
                }
            }

            if (string.IsNullOrEmpty(lang))
            {
                return (StringBuffer)"<pre><code>" + (escaped ? code : StringHelper.Escape(code, true)) + "\n</code></pre>";
            }

            return "<pre><code class=\""
                + Options.LangPrefix
                + StringHelper.Escape(lang, true)
                + "\">"
                + (escaped ? code : StringHelper.Escape(code, true))
                + "\n</code></pre>\n";
        }

        public virtual StringBuffer Blockquote(StringBuffer quote)
        {
            return (StringBuffer)"<blockquote>\n" + quote + "</blockquote>\n";
        }

        public virtual StringBuffer Html(StringBuffer html)
        {
            return html;
        }

        public virtual StringBuffer Heading(StringBuffer text, int level, string raw)
        {
            return (StringBuffer)"<h"
                + level
                + " id=\""
                + Options.HeaderPrefix
                + Regex.Replace(raw.ToLower(), @"[^\w]+", "-")
                + "\">"
                + text
                + "</h"
                + level
                + ">\n";
        }

        public virtual StringBuffer Hr()
        {
            return Options.XHtml ? "<hr/>\n" : "<hr>\n";
        }

        public virtual StringBuffer List(StringBuffer body, bool ordered)
        {
            var type = ordered ? "ol" : "ul";
            return (StringBuffer)"<" + type + ">\n" + body + "</" + type + ">\n";
        }

        public virtual StringBuffer ListItem(StringBuffer text)
        {
            return (StringBuffer)"<li>" + text + "</li>\n";
        }

        public virtual StringBuffer Paragraph(StringBuffer text)
        {
            return (StringBuffer)"<p>" + text + "</p>\n";
        }

        public virtual StringBuffer Table(StringBuffer header, StringBuffer body)
        {
            return (StringBuffer)"<table>\n"
                + "<thead>\n"
                + header
                + "</thead>\n"
                + "<tbody>\n"
                + body
                + "</tbody>\n"
                + "</table>\n";
        }

        public virtual StringBuffer TableRow(StringBuffer content)
        {
            return (StringBuffer)"<tr>\n" + content + "</tr>\n";
        }

        public virtual StringBuffer TableCell(StringBuffer content, TableCellFlags flags)
        {
            var type = flags.Header ? "th" : "td";
            var tag = flags.Align != Align.NotSpec
                ? (StringBuffer)"<" + type + " style=\"text-align:" + flags.Align.ToString().ToLower() + "\">"
                : (StringBuffer)"<" + type + ">";

            return tag + content + "</" + type + ">\n";
        }

        #endregion

        #region Span Level Renderer

        public virtual StringBuffer Strong(StringBuffer text)
        {
            return (StringBuffer)"<strong>" + text + "</strong>";
        }

        public virtual StringBuffer Em(StringBuffer text)
        {
            return (StringBuffer)"<em>" + text + "</em>";
        }

        public virtual StringBuffer CodeSpan(StringBuffer text)
        {
            return (StringBuffer)"<code>" + text + "</code>";
        }

        public virtual StringBuffer Br()
        {
            return Options.XHtml ? "<br/>" : "<br>";
        }

        public virtual StringBuffer Del(StringBuffer text)
        {
            return (StringBuffer)"<del>" + text + "</del>";
        }

        public virtual StringBuffer Link(StringBuffer href, StringBuffer title, StringBuffer text)
        {
            if (Options.Sanitize)
            {
                string prot = null;

                try
                {
                    prot = Regex.Replace(StringHelper.DecodeURIComponent(StringHelper.Unescape(href)), @"[^\w:]", String.Empty).ToLower();
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

            var result = (StringBuffer)"<a href=\"" + href + "\"";
            if (title != StringBuffer.Empty)
            {
                result = result + " title=\"" + title + "\"";
            }

            result = result + ">" + text + "</a>";
            return result;
        }

        public virtual StringBuffer Image(StringBuffer href, StringBuffer title, StringBuffer text)
        {
            var result = (StringBuffer)"<img src=\"" + href + "\" alt=\"" + text + "\"";
            if (!string.IsNullOrEmpty(title))
            {
                result = result + " title=\"" + title + "\"";
            }

            result += Options.XHtml ? "/>" : ">";
            return result;
        }

        public virtual StringBuffer Text(StringBuffer text)
        {
            return text;
        }

        #endregion

        #endregion

    }
}
