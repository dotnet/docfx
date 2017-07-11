// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class MarkdownParser : IMarkdownParser
    {
        public MarkdownParser(IMarkdownContext context, Options options, Dictionary<string, LinkObj> links)
        {
            Context = context;
            Options = options;
            Links = links;
        }

        public Options Options { get; }

        public IMarkdownContext Context { get; private set; }

        public Dictionary<string, LinkObj> Links { get; }

        public IMarkdownContext SwitchContext(IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return SwitchContextCore(context);
        }

        private IMarkdownContext SwitchContextCore(IMarkdownContext context)
        {
            var result = Context;
            Context = context;
            return result;
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        public ImmutableArray<IMarkdownToken> Tokenize(SourceInfo sourceInfo)
        {
            var markdown = Preprocess(sourceInfo.Markdown);
            if (sourceInfo.Markdown != markdown)
            {
                sourceInfo = sourceInfo.Copy(markdown);
            }
            return TokenizeCore(sourceInfo).ToImmutableArray();
        }

        private List<IMarkdownToken> TokenizeCore(SourceInfo sourceInfo)
        {
            var pc = new MarkdownParsingContext(sourceInfo);
            var tokens = new List<IMarkdownToken>();
            while (pc.CurrentMarkdown.Length > 0)
            {
                var token = ApplyRules(pc);
                if (token == null)
                {
                    throw new MarkdownParsingException("Cannot parse markdown: No rule match.", pc.ToSourceInfo());
                }
                else if (token.Rule is MarkdownTextBlockRule)
                {
                    pc.IsInParagraph = true;
                }
                else
                {
                    pc.IsInParagraph = false;
                }
                tokens.Add(token);
            }
            return tokens;
        }

        private IMarkdownToken ApplyRules(MarkdownParsingContext pc)
        {
            foreach (var r in Context.Rules)
            {
                try
                {
                    var token = r.TryMatch(this, pc);
                    if (token != null)
                    {
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    throw new MarkdownParsingException($"Cannot parse markdown: Rule {r.Name} is fault.", pc.ToSourceInfo(), ex);
                }
            }
            return null;
        }
    }
}
