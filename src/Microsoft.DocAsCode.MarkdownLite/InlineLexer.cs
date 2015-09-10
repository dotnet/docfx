// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    public class InlineResolverContext : IResolverContext { }

    /// <summary>
    /// Inline Lexer & Compiler
    /// </summary>
    public class InlineLexer
    {
        protected readonly Options Options;
        private readonly InlineRules _rules;
        private IDictionary<string, LinkObj> _links;
        private int _mangleCounter;
        private bool _inLink;
        private InlineResolverContext _context = new InlineResolverContext();
        public ResolversCollection<Resolver<StringBuffer>> InlineResolvers { get; private set; } = new ResolversCollection<Resolver<StringBuffer>>();

        public InlineLexer(Options options)
        {
            Options = options ?? new Options();

            _rules = GetDefaultInlineRule(Options);

            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Escape, _rules.Escape, ApplyEscape));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.AutoLink, _rules.AutoLink, ApplyAutoLink));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Url, _rules.Url, ApplyUrl));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Tag, _rules.Tag, ApplyTag));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Link, _rules.Link, ApplyLink));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.RefLink, _rules.RefLink, ApplyRefLinkOrNoLink));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.NoLink, _rules.NoLink, ApplyRefLinkOrNoLink));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Strong, _rules.Strong, ApplyStrong));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Em, _rules.Em, ApplyEm));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Code, _rules.Code, ApplyCode));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Br, _rules.Br, ApplyBr));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Del, _rules.Del, ApplyDel));
            InlineResolvers.Add(new Resolver<StringBuffer>(TokenName.Text, _rules.Text, ApplyText));
        }

        public void SetLinks(IDictionary<string, LinkObj> links)
        {
            if (links == null)
            {
                throw new ArgumentNullException("links");
            }

            _links = links;
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

        protected virtual InlineRules GetDefaultInlineRule(Options options)
        {
            if (options.Gfm)
            {
                if (options.Breaks)
                {
                    return new BreaksInlineRules();
                }
                else
                {
                    return new GfmInlineRules();
                }
            }
            else if (options.Pedantic)
            {
                return new PedanticInlineRules();
            }
            else
            {
                return new NormalInlineRules();
            }
        }

        protected virtual bool ApplyRules(ref string src, ref StringBuffer result)
        {
            foreach (var rule in InlineResolvers)
            {
                if (rule.Apply(ref src, ref result, _context))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool ApplyEscape(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += match.Groups[1].Value;
            return true;
        }

        protected virtual bool ApplyAutoLink(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            StringBuffer text;
            StringBuffer href;
            if (match.Groups[2].Value == "@")
            {
                text = match.Groups[1].Value[6] == ':'
                  ? Mangle(match.Groups[1].Value.Substring(7))
                  : Mangle(match.Groups[1].Value);
                href = Mangle("mailto:") + text;
            }
            else
            {
                text = StringHelper.Escape(match.Groups[1].Value);
                href = text;
            }
            result += Options.Renderer.Link(href, null, text);
            return true;
        }

        protected virtual bool ApplyUrl(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            StringBuffer text = StringHelper.Escape(match.Groups[1].Value);
            StringBuffer href = text;
            result += Options.Renderer.Link(href, null, text);
            return true;
        }

        protected virtual bool ApplyTag(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            if (!_inLink && Regexes.Lexers.StartHtmlLink.IsMatch(match.Groups[0].Value))
            {
                _inLink = true;
            }
            else if (_inLink && Regexes.Lexers.EndHtmlLink.IsMatch(match.Groups[0].Value))
            {
                _inLink = false;
            }
            result += Options.Sanitize
              ? (Options.Sanitizer != null)
                ? Options.Sanitizer(match.Groups[0].Value)
                : StringHelper.Escape(match.Groups[0].Value)
              : match.Groups[0].Value;
            return true;
        }

        protected virtual bool ApplyLink(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            _inLink = true;
            result += this.OutputLink(match, new LinkObj
            {
                Href = match.Groups[2].Value,
                Title = match.Groups[3].Value
            });
            _inLink = false;
            return true;
        }

        protected virtual bool ApplyRefLinkOrNoLink(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            var linkStr = NotEmpty(match, 2, 1).ReplaceRegex(Regexes.Lexers.WhiteSpaces, " ");

            LinkObj link;
            _links.TryGetValue(linkStr.ToLower(), out link);

            if (link == null || string.IsNullOrEmpty(link.Href))
            {
                result += match.Groups[0].Value[0];
                src = match.Groups[0].Value.Substring(1) + src;
                return true;
            }
            _inLink = true;
            result += OutputLink(match, link);
            _inLink = false;
            return true;
        }

        protected virtual bool ApplyStrong(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.Strong(ApplyRules(NotEmpty(match, 2, 1)));
            return true;
        }

        protected virtual bool ApplyEm(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.Em(ApplyRules(NotEmpty(match, 2, 1)));
            return true;
        }

        protected virtual bool ApplyCode(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.CodeSpan(StringHelper.Escape(match.Groups[2].Value, true));
            return true;
        }

        protected virtual bool ApplyBr(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.Br();
            return true;
        }

        protected virtual bool ApplyDel(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.Del(ApplyRules(match.Groups[1].Value));
            return true;
        }

        protected virtual bool ApplyText(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            result += Options.Renderer.Text(StringHelper.Escape(Smartypants(match.Groups[0].Value)));
            return true;
        }

        /// <summary>
        /// Compile Link
        /// </summary>
        protected virtual StringBuffer OutputLink(Match match, LinkObj link)
        {
            string href = StringHelper.Escape(link.Href),
            title = !string.IsNullOrEmpty(link.Title) ? StringHelper.Escape(link.Title) : null;

            return match.Groups[0].Value[0] != '!'
                ? Options.Renderer.Link(href, title, ApplyRules(match.Groups[1].Value))
                : Options.Renderer.Image(href, title, StringHelper.Escape(match.Groups[1].Value));
        }


        protected virtual string NotEmpty(Match match, int index1, int index2)
        {
            if (match.Groups.Count > index1 && !string.IsNullOrEmpty(match.Groups[index1].Value))
            {
                return match.Groups[index1].Value;
            }
            return match.Groups[index2].Value;
        }

        /// <summary>
        /// Mangle Links
        /// </summary>
        protected virtual StringBuffer Mangle(string text)
        {
            if (!Options.Mangle) return text;
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
            if (!Options.Smartypants) return text;

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
