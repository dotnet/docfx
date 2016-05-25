// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

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
                var token = (from r in Context.Rules
                             select r.TryMatch(this, pc)).FirstOrDefault(t => t != null);
                if (token == null)
                {
                    throw new InvalidOperationException($"Cannot parse markdown for file {sourceInfo.File}, line {pc.LineNumber}.");
                }
                tokens.Add(token);
            }
            return tokens;
        }
    }
}
