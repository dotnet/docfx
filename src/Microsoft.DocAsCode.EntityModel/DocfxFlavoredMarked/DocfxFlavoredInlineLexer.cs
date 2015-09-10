// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class DocfxFlavoredInlineLexer : InlineLexer
    {
        private static readonly Regex _xrefRegex = new Regex(@"^@(?:(['""])(\s*\S+[\s\S]*?)\1|(?:([^'""][\s\S]*?))(?=[\s@]|$))", RegexOptions.Compiled);
        private static readonly Regex _inlineTextRegex = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`@]| {2,}\n|$)", RegexOptions.Compiled);
        //[!inc[title](path)]
        private static readonly Regex _inlineIncludeRegex = new Regex(DocfxFlavoredIncHelper.InlineIncRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly DocfxFlavoredIncHelper _inclusionHelper = new DocfxFlavoredIncHelper();

        public Stack<string> Parents { get; set; }

        public DocfxFlavoredInlineLexer(Options options) : base(options)
        {
            var linkResolver = this.InlineResolvers[TokenName.Link];
            Resolver<StringBuffer> xrefResolver = new Resolver<StringBuffer>("XREF", _xrefRegex, ApplyXRef);
            this.InlineResolvers.InsertAfter(linkResolver, xrefResolver);

            Resolver<StringBuffer> incResolver = new Resolver<StringBuffer>("INC", _inlineIncludeRegex, ApplyInclude);
            this.InlineResolvers.InsertBefore(linkResolver, incResolver);

            Resolver<StringBuffer> inlineTextResolver = this.InlineResolvers[TokenName.Text];
            inlineTextResolver.Regex = _inlineTextRegex;
        }

        public string ApplyRules(string src, Stack<string> parents)
        {
            Parents = parents;
            var result = base.ApplyRules(src);
            return result;
        }

        protected virtual bool ApplyXRef(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            // @String=>cap[3]=String, @'string'=>cap[2]=string
            // For cross-reference, add ~/ prefix
            var content = string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
            result += $"<xref href=\"{StringHelper.Escape(content)}\"></xref>";
            return true;
        }

        protected virtual bool ApplyInclude(Match match, IResolverContext context, ref string src, ref StringBuffer result)
        {
            // [!inc[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            // 3. Apply inline rules to the included content
            var resolved = _inclusionHelper.Load(path, title, value, Parents, null, ApplyRules, MarkdownNodeType.Inline, (DocfxFlavoredOptions)Options);
            result += resolved;
            return true;
        }
    }
}
