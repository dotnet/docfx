// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using Utility;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class DfmXrefInlineRule : IMarkdownRule
    {
        private static readonly Regex _xrefRegex = new Regex(@"^@(?:(['""])(\s*\S+[\s\S]*?)\1|(?:([^'""][\s\S]*?))(?=[\s@]|$))", RegexOptions.Compiled);
        public string Name => "XREF";
        public virtual Regex Xref => _xrefRegex;
        public IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Xref.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // @String=>cap[3]=String, @'string'=>cap[2]=string
            // For cross-reference, add ~/ prefix
            var content = string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
            return new DfmXrefInlineToken(this, content, null, null);
        }
    }

    public class DfmIncludeInlineRule: IMarkdownRule
    {
        public string Name => "INC";
        private static readonly Regex _inlineIncludeRegex = new Regex(DocfxFlavoredIncHelper.InlineIncRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public virtual Regex Include => _inlineIncludeRegex;

        public IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Include.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!inc[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            // 3. Apply inline rules to the included content
            return new DfmIncludeInlineToken(this, path, value, title, match.Groups[0].Value);
        }
    }

    public class DfmTextInlineRule : MarkdownTextInlineRule
    {
        private static readonly Regex _inlineTextRegex = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`@]| {2,}\n|$)", RegexOptions.Compiled);
        public override Regex Text => _inlineTextRegex;
    }
}
