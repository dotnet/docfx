// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureEngineBuilder : GfmEngineBuilder
    {
        public AzureEngineBuilder(Options options) : base(options)
        {
            BuildRules();
            CreateRewriters();
        }

        protected override void BuildRules()
        {
            base.BuildRules();
            BuildBlockRules();
            BuildInlineRules();
        }

        protected override void BuildBlockRules()
        {
            base.BuildBlockRules();
            var blockRules = BlockRules.ToList();
            var index = blockRules.FindLastIndex(s => s is MarkdownNewLineBlockRule);
            if (index < 0)
            {
                throw new ArgumentException($"{nameof(MarkdownNewLineBlockRule)} should exist!");
            }
            blockRules.Insert(index + 1, new AzureIncludeBlockRule());
            blockRules.Insert(index + 2, new AzureNoteBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new AzureParagraphBlockRule();

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            blockRules[markdownBlockQuoteIndex] = new AzureBlockquoteBlockRule();

            BlockRules = blockRules.ToImmutableList();
        }

        protected override void BuildInlineRules()
        {
            base.BuildInlineRules();
            var inlineRules = InlineRules.ToList();
            var index = inlineRules.FindLastIndex(s => s is MarkdownLinkInlineRule);
            if (index < 0)
            {
                throw new ArgumentException($"{nameof(MarkdownLinkInlineRule)} should exist!");
            }
            inlineRules.Insert(index, new AzureIncludeInlineRule());
            InlineRules = inlineRules.ToImmutableList();
        }

        protected void CreateRewriters()
        {
            Rewriter = MarkdownTokenRewriterFactory.Composite(
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureIncludeInlineToken t) => new DfmIncludeInlineToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.Raw, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureIncludeBlockToken t) => new DfmIncludeBlockToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.Raw, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureNoteBlockToken t) => new DfmNoteBlockToken(t.Rule, t.Context, t.NoteType.Substring("AZURE.".Length), t.Content, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureBlockquoteBlockToken t) => new DfmBlockquoteBlockToken(t.Rule, t.Context, t.Tokens, t.RawMarkdown)
                        )
                    );
        }
    }
}
