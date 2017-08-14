// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownEngine : IMarkdownEngine
    {
        private static readonly char[] NewLineOrTab = { '\n', '\t' };
        private static readonly string[] Spaces = { "    ", "   ", "  ", " " };

        public MarkdownEngine(IMarkdownContext context, object renderer, Options options)
            : this(context, null, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        public MarkdownEngine(IMarkdownContext context, IMarkdownTokenRewriter rewriter, object renderer, Options options)
            : this(context, rewriter, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        protected MarkdownEngine(IMarkdownContext context, IMarkdownTokenRewriter rewriter, object renderer, Options options, Dictionary<string, LinkObj> links)
        {
            Context = context;
            Rewriter = rewriter ?? MarkdownTokenRewriterFactory.Null;
            RendererImpl = renderer;
            Options = options;
            Links = links;
        }

        public object RendererImpl { get; }

        public Options Options { get; }

        public IMarkdownContext Context { get; private set; }

        public IMarkdownTokenRewriter Rewriter { get; }

        public IMarkdownTokenTreeValidator TokenTreeValidator { get; set; }

        [Obsolete]
        public IMarkdownTokenAggregator TokenAggregator { get; set; }

        public ImmutableList<IMarkdownTokenAggregator> TokenAggregators { get; set; }

        public Dictionary<string, LinkObj> Links { get; }

        public int MaxExtractCount { get; set; } = 1;

        public static string Normalize(string markdown)
        {
            var result = markdown
                .ReplaceRegex(Regexes.Lexers.NormalizeNewLine, "\n")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n");
            return Regex.Replace(result, "\\t", m =>
            {
                if (m.Index == 0)
                {
                    return Spaces[0];
                }
                var index = result.LastIndexOfAny(NewLineOrTab, m.Index - 1);
                return Spaces[(m.Index - index - 1) % 4];
            });
        }

        public StringBuffer Mark(SourceInfo sourceInfo, IMarkdownContext context)
        {
            var result = StringBuffer.Empty;
            var parser = Parser;
            if (context != null)
            {
                parser.SwitchContext(context);
            }
            var preprocessedSourceInfo = sourceInfo.Copy(Preprocess(sourceInfo.Markdown));

            var tokens = parser.Tokenize(preprocessedSourceInfo);
            if (parser.Context is MarkdownBlockContext)
            {
                tokens = TokenHelper.CreateParagraghs(
                    parser,
                    MarkdownParagraphBlockRule.Instance,
                    tokens,
                    true,
                    preprocessedSourceInfo);
            }

            // resolve two phase token
            tokens = RewriteTokens(
                tokens,
                sourceInfo.File,
                new MarkdownRewriteEngine(
                    this,
                    MarkdownTokenRewriterFactory.Loop(
                        MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, TwoPhaseBlockToken>(
                            (e, t) => t.Extract(parser)),
                    MaxExtractCount + 1)));

            // Aggregate tokens.
            foreach (var agg in TokenAggregators)
            {
                tokens = RewriteTokens(
                    tokens,
                    sourceInfo.File,
                    new MarkdownAggregateEngine(
                        this,
                        agg));
            }

            // customized rewriter.
            tokens = RewriteTokens(
                tokens,
                sourceInfo.File,
                RewriteEngine);

            if (Options.ShouldFixId)
            {
                // fix id.
                var idTable = new Dictionary<string, int>();
                tokens = RewriteTokens(
                    tokens,
                    sourceInfo.File,
                    new MarkdownRewriteEngine(
                        this,
                        MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, MarkdownHeadingBlockToken>(
                            (e, t) => t.RewriteId(idTable))));
            }

            if (TokenTreeValidator != null)
            {
                TokenTreeValidator.Validate(tokens);
            }

            var renderer = Renderer;
            foreach (var token in tokens)
            {
                result += renderer.Render(token);
            }
            return result;
        }

        private static ImmutableArray<IMarkdownToken> RewriteTokens(ImmutableArray<IMarkdownToken> tokens, string file, IMarkdownRewriteEngine rewriteEngine)
        {
            using (new MarkdownTokenValidatorContext(rewriteEngine, file))
            {
                rewriteEngine.Initialize();
                tokens = rewriteEngine.Rewrite(tokens);
                rewriteEngine.Complete();
            }
            return tokens;
        }

        public virtual string Markup(string markdown, string file)
        {
            var normalized = Normalize(markdown);
            return Mark(SourceInfo.Create(normalized, file), null).ToString();
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        public virtual IMarkdownParser Parser =>
            new MarkdownParser(Context, Options, Links);

        public virtual IMarkdownRewriteEngine RewriteEngine =>
            new MarkdownRewriteEngine(this, Rewriter);

        public virtual IMarkdownRenderer Renderer =>
            new MarkdownRendererAdapter(this, RendererImpl, Options, Links);
    }
}
