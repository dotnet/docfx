// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Inline Lexer & Compiler
    /// </summary>
    public class InlineLexer
    {
        private readonly Options _options;
        private readonly InlineRules _rules;
        private readonly IDictionary<string, LinkObj> _links;
        private int _mangleCounter;
        private bool _inLink;

        public InlineLexer(IDictionary<string, LinkObj> links, Options options)
        {
            if (links == null)
            {
                throw new ArgumentNullException("links");
            }
            _options = options ?? new Options();

            _links = links;
            _rules = new NormalInlineRules();

            if (_options.Gfm)
            {
                if (_options.Breaks)
                {
                    _rules = new BreaksInlineRules();
                }
                else
                {
                    _rules = new GfmInlineRules();
                }
            }
            else if (_options.Pedantic)
            {
                _rules = new PedanticInlineRules();
            }
            else
            {
                _rules = new NormalInlineRules();
            }
        }

        /// <summary>
        /// Lexing/Compiling
        /// </summary>
        public virtual StringBuffer ApplyRules(string src)
        {
            var result = StringBuffer.Empty;

            while (!string.IsNullOrEmpty(src))
            {
                if (!ApplyRules(ref src, ref result))
                {
                    throw new Exception("Cannot find suitable rule for byte: " + ((int)src[0]).ToString());
                }
            }

            return result;
        }

        protected virtual bool ApplyRules(ref string src, ref StringBuffer result)
        {
            return
                // escape
                ApplyEscape(ref src, ref result) ||
                // autolink
                ApplyAutoLink(ref src, ref result) ||
                // url (gfm)
                ApplyUrl(ref src, ref result) ||
                // tag
                ApplyTag(ref src, ref result) ||
                // link
                ApplyLink(ref src, ref result) ||
                // reflink, nolink
                ApplyRefLinkOrNoLink(ref src, ref result) ||
                // strong
                ApplyStrong(ref src, ref result) ||
                // em
                ApplyEm(ref src, ref result) ||
                // code
                ApplyCode(ref src, ref result) ||
                // br
                ApplyBr(ref src, ref result) ||
                // del (gfm)
                ApplyDel(ref src, ref result) ||
                // text
                ApplyText(ref src, ref result);
        }

        protected virtual bool ApplyEscape(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Escape.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += cap[1];
                return true;
            }
            return false;
        }

        protected virtual bool ApplyAutoLink(ref string src, ref StringBuffer result)
        {
            var cap = _rules.AutoLink.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                StringBuffer text;
                StringBuffer href;
                if (cap[2] == "@")
                {
                    text = cap[1][6] == ':'
                      ? Mangle(cap[1].Substring(7))
                      : Mangle(cap[1]);
                    href = Mangle("mailto:") + text;
                }
                else
                {
                    text = StringHelper.Escape(cap[1]);
                    href = text;
                }
                result += _options.Renderer.Link(href, null, text);
                return true;
            }
            return false;
        }

        protected virtual bool ApplyUrl(ref string src, ref StringBuffer result)
        {
            string[] cap;
            if (!_inLink && (cap = _rules.Url.Apply(src)).Length > 0)
            {
                src = src.Substring(cap[0].Length);
                StringBuffer text = StringHelper.Escape(cap[1]);
                StringBuffer href = text;
                result += _options.Renderer.Link(href, null, text);
                return true;
            }
            return false;
        }

        protected virtual bool ApplyTag(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Tag.Apply(src);
            if (cap.Length > 0)
            {
                if (!_inLink && Regexes.Lexers.StartHtmlLink.IsMatch(cap[0]))
                {
                    _inLink = true;
                }
                else if (_inLink && Regexes.Lexers.EndHtmlLink.IsMatch(cap[0]))
                {
                    _inLink = false;
                }
                src = src.Substring(cap[0].Length);
                result += _options.Sanitize
                  ? (_options.Sanitizer != null)
                    ? _options.Sanitizer(cap[0])
                    : StringHelper.Escape(cap[0])
                  : cap[0];
                return true;
            }
            return false;
        }

        protected virtual bool ApplyLink(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Link.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                _inLink = true;
                result += this.OutputLink(cap, new LinkObj
                {
                    Href = cap[2],
                    Title = cap[3]
                });
                _inLink = false;
                return true;
            }
            return false;
        }

        protected virtual bool ApplyRefLinkOrNoLink(ref string src, ref StringBuffer result)
        {
            string[] cap;
            if ((cap = _rules.RefLink.Apply(src)).Length > 0 || (cap = _rules.NoLink.Apply(src)).Length > 0)
            {
                src = src.Substring(cap[0].Length);
                var linkStr = (StringHelper.NotEmpty(cap, 2, 1)).ReplaceRegex(Regexes.Lexers.WhiteSpaces, " ");

                LinkObj link;
                _links.TryGetValue(linkStr.ToLower(), out link);

                if (link == null || string.IsNullOrEmpty(link.Href))
                {
                    result += cap[0][0];
                    src = cap[0].Substring(1) + src;
                    return true;
                }
                _inLink = true;
                result += OutputLink(cap, link);
                _inLink = false;
                return true;
            }
            return false;
        }

        protected virtual bool ApplyStrong(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Strong.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.Strong(ApplyRules(StringHelper.NotEmpty(cap, 2, 1)));
                return true;
            }
            return false;
        }

        protected virtual bool ApplyEm(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Em.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.Em(ApplyRules(StringHelper.NotEmpty(cap, 2, 1)));
                return true;
            }
            return false;
        }

        protected virtual bool ApplyCode(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Code.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.CodeSpan(StringHelper.Escape(cap[2], true));
                return true;
            }
            return false;
        }

        protected virtual bool ApplyBr(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Br.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.Br();
                return true;
            }
            return false;
        }

        protected virtual bool ApplyDel(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Del.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.Del(ApplyRules(cap[1]));
                return true;
            }
            return false;
        }

        protected virtual bool ApplyText(ref string src, ref StringBuffer result)
        {
            var cap = _rules.Text.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                result += _options.Renderer.Text(StringHelper.Escape(Smartypants(cap[0])));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compile Link
        /// </summary>
        protected virtual StringBuffer OutputLink(IList<string> cap, LinkObj link)
        {
            string href = StringHelper.Escape(link.Href),
            title = !string.IsNullOrEmpty(link.Title) ? StringHelper.Escape(link.Title) : null;

            return cap[0][0] != '!'
                ? _options.Renderer.Link(href, title, ApplyRules(cap[1]))
                : _options.Renderer.Image(href, title, StringHelper.Escape(cap[1]));
        }

        /// <summary>
        /// Mangle Links
        /// </summary>
        protected virtual StringBuffer Mangle(string text)
        {
            if (!_options.Mangle) return text;
            var result = StringBuffer.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i].ToString();
                if ((_mangleCounter++ & 1) == 0)
                {
                    result = result + "&#x" + Convert.ToString((int)ch[0], 16) + ";";
                }
                else
                {
                    result = result + "&#" + ch + ";";
                }
            }

            return result;
        }

        /// <summary>
        /// Smartypants Transformations
        /// </summary>
        protected virtual string Smartypants(string text)
        {
            if (!_options.Smartypants) return text;

            return text
                // em-dashes
                .Replace("---", "\u2014")
                // en-dashes
                .Replace("--", "\u2013")
                // opening singles
                .ReplaceRegex(Regexes.Inline.Smartypants.OpeningSingles, "$1\u2018")
                // closing singles & apostrophes
                .Replace("'", "\u2019")
                // opening doubles
                .ReplaceRegex(Regexes.Inline.Smartypants.OpeningDoubles, "$1\u201c")
                // closing doubles
                .Replace("\"", "\u201d")
                // ellipses
                .Replace("...", "\u2026");
        }
    }
}
