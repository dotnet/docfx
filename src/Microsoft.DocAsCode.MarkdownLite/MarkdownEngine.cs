﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    public class MarkdownEngine : IMarkdownEngine
    {
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

        public Dictionary<string, LinkObj> Links { get; }

        public int MaxExtractCount { get; set; } = 1;

        public static string Normalize(string markdown)
        {
            return markdown
                .ReplaceRegex(Regexes.Lexers.NormalizeNewLine, "\n")
                .Replace("\t", "    ")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n");
        }

        public StringBuffer Mark(SourceInfo sourceInfo, IMarkdownContext context)
        {
            var result = StringBuffer.Empty;
            var parser = Parser;
            if (context != null)
            {
                parser.SwitchContext(context);
            }
            var tokens = parser.Tokenize(sourceInfo.Copy(Preprocess(sourceInfo.Markdown)));
            var internalRewriteEngine =
                new MarkdownRewriteEngine(
                    this,
                    MarkdownTokenRewriterFactory.Loop(
                        MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, TwoPhaseBlockToken>(
                            (e, t) => t.Extract(parser)),
                    MaxExtractCount + 1));
            internalRewriteEngine.Initialize();
            tokens = internalRewriteEngine.Rewrite(tokens);
            internalRewriteEngine.Complete();

            var idTable = new Dictionary<string, int>();
            var idRewriteEngine = new MarkdownRewriteEngine(
                this,
                MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, MarkdownHeadingBlockToken>(
                    (e, t) => t.RewriteId(idTable)));
            idRewriteEngine.Initialize();
            tokens = idRewriteEngine.Rewrite(tokens);
            idRewriteEngine.Complete();

            var rewriteEngine = RewriteEngine;
            rewriteEngine.Initialize();
            tokens = rewriteEngine.Rewrite(tokens);
            rewriteEngine.Complete();

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
