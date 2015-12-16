// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmEngine : MarkdownEngine
    {
        internal const string FilePathStackKey = "FilePathStack";

        public DfmEngine(IMarkdownContext context, IMarkdownRewriter rewriter, object renderer, Options options)
            : base(context, rewriter, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        public string Markup(string src, string path)
        {
            if (string.IsNullOrEmpty(src) && string.IsNullOrEmpty(path)) return string.Empty;
            // bug : Environment.CurrentDirectory = c:\a, path = d:\b, MakeRelativePath is not work ...
            path = PathUtility.MakeRelativePath(Environment.CurrentDirectory, path);
            return InternalMarkup(src, ImmutableStack<string>.Empty.Push(path));
        }

        public string InternalMarkup(string src, ImmutableStack<string> parents)
        {
            DfmEngine engine = new DfmEngine(Context, Rewriter, Renderer, Options);
            return Mark(Normalize(src), Context.CreateContext(Context.Variables.SetItem(FilePathStackKey, parents))).ToString();
        }

        public string InternalMarkup(string src, IMarkdownContext context)
        {
            DfmEngine engine = new DfmEngine(context, Rewriter, Renderer, Options);
            return Mark(Normalize(src), context).ToString();
        }

        public override IMarkdownParser Parser => new DfmParser(Context, Options, Links);

        public override IMarkdownRenderer Renderer => new DfmRendererAdapter(this, RendererImpl, Options, Links);
    }

    public class DfmParser : MarkdownParser
    {
        public ImmutableStack<string> FilePathStack => (ImmutableStack<string>)Context.Variables[DfmEngine.FilePathStackKey];

        public DfmParser(IMarkdownContext context, Options options, Dictionary<string, LinkObj> links)
            : base(context, options, links)
        {
        }
    }

    public class DfmRendererAdapter : MarkdownRendererAdapter
    {
        public DfmRendererAdapter(DfmEngine engine, object renderer, Options options, Dictionary<string, LinkObj> links)
            : base(engine, renderer, options, links)
        {
            Engine = engine;
        }

        public new DfmEngine Engine { get; }

        public ImmutableStack<string> GetFilePathStack(IMarkdownContext context)
        {
            return (ImmutableStack<string>)context.Variables[DfmEngine.FilePathStackKey]; ;
        }

        public IMarkdownContext SetFilePathStack(IMarkdownContext context, ImmutableStack<string> filePathStack)
        {
            return context.CreateContext(context.Variables.SetItem(DfmEngine.FilePathStackKey, filePathStack));
        }
    }

    public class DfmEngineBuilder : GfmEngineBuilder
    {
        public DfmEngineBuilder(Options options) : base(options)
        {
            var inlineRules = InlineRules.ToList();

            var index = inlineRules.FindLastIndex(s => s is MarkdownLinkInlineRule);
            if (index < 0) throw new ArgumentException("MarkdownLinkInlineRule should exist!");
            inlineRules.Insert(index + 1, new DfmXrefInlineRule());
            inlineRules.Insert(index, new DfmIncludeInlineRule());
            index = inlineRules.FindIndex(s => s is MarkdownTextInlineRule);
            inlineRules[index] = new DfmTextInlineRule();

            var blockRules = BlockRules.ToList();
            index = blockRules.FindLastIndex(s => s is MarkdownNewLineBlockRule);
            if (index < 0) throw new ArgumentException("MarkdownNewLineBlockRule should exist!");
            blockRules.Insert(index + 1, new DfmIncludeBlockRule());
            blockRules.Insert(index + 2, new DfmYamlHeaderBlockRule());
            blockRules.Insert(index + 3, new DfmSectionBeginBlockRule());
            blockRules.Insert(index + 4, new DfmFencesBlockRule());
            blockRules.Insert(index + 5, new DfmNoteBlockRule());
            blockRules.Insert(index + 6, new DfmSectionEndBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new DfmParagraphBlockRule();

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            blockRules[markdownBlockQuoteIndex] = new DfmBlockquoteBlockRule();

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();
        }

        public DfmEngine CreateDfmEngine(object renderer)
        {
            return new DfmEngine(CreateParseContext(), Rewriter, renderer, Options);
        }

        public override IMarkdownEngine CreateEngine(object renderer)
        {
            return CreateDfmEngine(renderer);
        }
    }
}
