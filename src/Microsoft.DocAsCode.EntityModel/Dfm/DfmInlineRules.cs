// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmXrefInlineRule : IMarkdownRule
    {
        private static readonly Regex _xrefRegex = new Regex(@"^@(?:(['""])(\s*?\S+?[\s\S]*?)\1|(?:([^'""][\S]*?))(?=[.,;:!?~\s]{2,}|[.,;:!?~]*$|\s))", RegexOptions.Compiled);
        public string Name => "XREF";

        /// <summary>
        /// XREF regex:
        ///     1. If content after `@` is wrapped by `'` or `"`,  it contains any character including white space
        ///     2. If content after `@` is not wrapped by `'` or `"`, it ends when
        ///         a. line ends
        ///         b. meets whitespaces
        ///         c. line ends with `.`, `,`, `;`, `:`, `!`, `?` and `~`
        ///         d. meets 2 times or more `.`, `,`, `;`, `:`, `!`, `?` and `~`
        /// </summary>
        public virtual Regex Xref => _xrefRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
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
            return new DfmXrefInlineToken(this, engine.Context, content, null, null);
        }
    }

    public class DfmEmailInlineRule : IMarkdownRule
    {
        private static readonly Regex _emailRegex = new Regex(@"^\s*[\w._%+-]*[\w_%+-]@[\w.-]+\.[\w]{2,}\b", RegexOptions.Compiled);
        public string Name => "Email";
        
        public virtual Regex Xref => _emailRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Xref.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownTextToken(this, engine.Context, match.Groups[0].Value);
        }
    }

    public class DfmIncludeInlineRule : IMarkdownRule
    {
        public string Name => "INCLUDE";
        private static readonly Regex _inlineIncludeRegex = new Regex(DocfxFlavoredIncHelper.InlineIncRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public virtual Regex Include => _inlineIncludeRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Include.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            // 3. Apply inline rules to the included content
            return new DfmIncludeInlineToken(this, engine.Context, path, value, title, match.Groups[0].Value);
        }
    }

    public class DfmTextInlineRule : MarkdownTextInlineRule
    {
        private static readonly Regex _inlineTextRegex = new Regex(@"^[\s\S]+?(?=\S*@|[\\<!\[_*`]| {2,}\n|$)", RegexOptions.Compiled);

        /// <summary>
        /// Override the one in MarkdownLite, difference is:
        /// If there is a `@` following `.`, `,`, `;`, `:`, `!`, `?` or whitespace, exclude it as it is a xref
        /// </summary>
        public override Regex Text => _inlineTextRegex;
    }
}
