// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Immutable;
    using System.Composition.Hosting;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmEngineBuilder : GfmEngineBuilder
    {
        public const string MarkdownStyleCopFileName = "md.stylecop";

        public DfmEngineBuilder(Options options, CompositionHost host = null) : base(options)
        {
            var inlineRules = InlineRules.ToList();

            var index = inlineRules.FindLastIndex(s => s is MarkdownLinkInlineRule);
            if (index < 0) throw new ArgumentException("MarkdownLinkInlineRule should exist!");
            inlineRules.Insert(index + 1, new DfmXrefInlineRule());
            inlineRules.Insert(index + 1, new DfmEmailInlineRule());
            inlineRules.Insert(index, new DfmIncludeInlineRule());
            index = inlineRules.FindIndex(s => s is MarkdownTextInlineRule);
            inlineRules[index] = new DfmTextInlineRule();

            var blockRules = BlockRules.ToList();
            index = blockRules.FindLastIndex(s => s is MarkdownNewLineBlockRule);
            if (index < 0) throw new ArgumentException("MarkdownNewLineBlockRule should exist!");
            blockRules.Insert(index + 1, new DfmIncludeBlockRule());
            blockRules.Insert(index + 2, new DfmYamlHeaderBlockRule());
            blockRules.Insert(index + 3, new DfmSectionBlockRule());
            blockRules.Insert(index + 4, new DfmFencesBlockRule());
            blockRules.Insert(index + 5, new DfmNoteBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new DfmParagraphBlockRule();

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            blockRules[markdownBlockQuoteIndex] = new DfmBlockquoteBlockRule();

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();

            Rewriter = InitMarkdownStyleCop(host);
        }

        private static IMarkdownTokenRewriter InitMarkdownStyleCop(CompositionHost host)
        {
            try
            {
                if (File.Exists(MarkdownStyleCopFileName))
                {
                    var rules = JsonUtility.Deserialize<MarkdownTagValidationRule[]>(MarkdownStyleCopFileName);
                    var builder = new MarkdownRewriterBuilder(host);
                    builder.AddValidators(rules);
                    return builder.Create();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown stylecop, details:{Environment.NewLine}{ex.ToString()}");
            }
            return null;
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
