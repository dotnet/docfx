// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class DfmEngineBuilder : GfmEngineBuilder
    {
        private readonly string _baseDir;
        private IReadOnlyList<string> _fallbackFolders;

        public DfmEngineBuilder(Options options, string baseDir = null, string templateDir = null, IReadOnlyList<string> fallbackFolders = null)
            : this(options, baseDir, templateDir, fallbackFolders, null)
        {
        }

        public DfmEngineBuilder(Options options, string baseDir, string templateDir, IReadOnlyList<string> fallbackFolders, ICompositionContainer container) : base(options)
        {
            _baseDir = baseDir ?? string.Empty;
            _fallbackFolders = fallbackFolders ?? new List<string>();
            var inlineRules = InlineRules.ToList();

            // xref auto link must be before MarkdownAutoLinkInlineRule
            var index = inlineRules.FindIndex(s => s is MarkdownAutoLinkInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownAutoLinkInlineRule should exist!");
            }
            inlineRules.Insert(index, new DfmXrefAutoLinkInlineRule());

            index = inlineRules.FindLastIndex(s => s is MarkdownLinkInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownLinkInlineRule should exist!");
            }
            inlineRules.Insert(index + 1, new DfmXrefShortcutInlineRule());
            inlineRules.Insert(index + 1, new DfmEmailInlineRule());
            inlineRules.Insert(index + 1, new DfmFencesInlineRule());

            // xref link inline rule must be before MarkdownLinkInlineRule
            inlineRules.Insert(index, new DfmIncludeInlineRule());

            index = inlineRules.FindIndex(s => s is MarkdownTextInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownTextInlineRule should exist!");
            }
            inlineRules[index] = new DfmTextInlineRule();

            var blockRules = BlockRules.ToList();
            index = blockRules.FindLastIndex(s => s is MarkdownCodeBlockRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownNewLineBlockRule should exist!");
            }

            blockRules.InsertRange(
                index + 1,
                new IMarkdownRule[]
                {
                    new DfmIncludeBlockRule(),
                    new DfmVideoBlockRule(),
                    new DfmYamlHeaderBlockRule(),
                    new DfmSectionBlockRule(),
                    new DfmFencesBlockRule(),
                    new DfmNoteBlockRule()
                });

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            if (markdownBlockQuoteIndex < 0)
            {
                throw new ArgumentException("MarkdownBlockquoteBlockRule should exist!");
            }
            blockRules[markdownBlockQuoteIndex] = new DfmBlockquoteBlockRule();

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();

            Rewriter = InitMarkdownStyle(container, baseDir, templateDir);
        }

        private static IMarkdownTokenRewriter InitMarkdownStyle(ICompositionContainer container, string baseDir, string templateDir)
        {
            try
            {
                return MarkdownValidatorBuilder.Create(container, baseDir, templateDir).CreateRewriter();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.ToString()}");
            }
            return null;
        }

        public DfmEngine CreateDfmEngine(object renderer)
        {
            return new DfmEngine(CreateParseContext().SetBaseFolder(_baseDir ?? string.Empty).SetFallbackFolders(_fallbackFolders), Rewriter, renderer, Options)
            {
                TokenTreeValidator = TokenTreeValidator,
            };
        }

        public override IMarkdownEngine CreateEngine(object renderer)
        {
            return CreateDfmEngine(renderer);
        }
    }
}
