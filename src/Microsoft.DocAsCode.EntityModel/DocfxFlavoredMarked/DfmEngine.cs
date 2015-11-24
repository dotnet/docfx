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

    public class DfmEngine: MarkdownEngine
    {
        public Stack<string> Parents { get; set; } = new Stack<string>();

        public DfmEngine(IMarkdownContext context, object renderer, Options options) : base(context, renderer, options)
        {
        }

        public string Markup(string src, string path)
        {
            if (string.IsNullOrEmpty(src) && string.IsNullOrEmpty(path)) return string.Empty;
            path = PathUtility.MakeRelativePath(Environment.CurrentDirectory, path);
            Parents.Push(path);
            return Markup(src);
        }

        public string InternalMarkup(string src, Stack<string> parents)
        {
            DfmEngine engine = new DfmEngine(Context, Renderer, Options);
            engine.Parents = parents;
            return engine.Markup(src);
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
            blockRules.Insert(index + 5, new DfmSectionEndBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new DfmParagraphBlockRule();

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();
        }

        public new DfmEngine CreateEngine(object renderer)
        {
            return new DfmEngine(CreateParseContext(), renderer, Options);
        }
    }
}
