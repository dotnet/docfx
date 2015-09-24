// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public class DocfxFlavoredLexer : Lexer
    {
        // Block inc must start with [!inc and end in a line
        private static readonly Regex _incRegex = new Regex($"{DocfxFlavoredIncHelper.InlineIncRegexString}\\s*(\\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex _yamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline);

        public DocfxFlavoredLexer(Options options) : base(options)
        {
            var codeResolver = this.BlockResolvers[TokenName.Code];
            Resolver<TokensResult> blockIncludeResolver = new Resolver<TokensResult>("INC", _incRegex, ApplyBlockInclude);
            Resolver<TokensResult> yamlHeaderResolver = new Resolver<TokensResult>("YamlHeader", _yamlHeaderRegex, ApplyYamlHeader, (context) => ((BlockResolverContext)context).top);
            this.BlockResolvers.InsertBefore(codeResolver, blockIncludeResolver);
            this.BlockResolvers.InsertBefore(codeResolver, yamlHeaderResolver);
        }

        protected virtual bool ApplyYamlHeader(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                    YamlUtility.Deserialize<Dictionary<string, object>>(reader);
            }
            catch (Exception)
            {
                return false;
            }

            tokens.Add(new Token
            {
                Type = TokenTypes.Html,
                Text = $"<yamlheader>{StringHelper.Escape(value)}</yamlheader>"
            });
            return true;
        }

        protected virtual bool ApplyBlockInclude(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            // [!inc[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            tokens.Add(new Token
            {
                Type = TokenTypes.Html,
                Pre = true,
                Text = $"<inc src='{StringHelper.Escape(path)}' title='{StringHelper.Escape(title)}'>{StringHelper.Escape(value)}</inc>"
            });
            return true;
        }
    }
}
