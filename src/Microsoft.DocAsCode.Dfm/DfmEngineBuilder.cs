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
        public const string DefaultValidatorName = "default";

        private readonly string _baseDir;

        public DfmEngineBuilder(Options options, string baseDir = null) : base(options)
        {
            _baseDir = baseDir ?? string.Empty;
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

            // xref link inline rule must be before MarkdownLinkInlineRule
            inlineRules.Insert(index, new DfmIncludeInlineRule());

            index = inlineRules.FindIndex(s => s is MarkdownTextInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownTextInlineRule should exist!");
            }
            inlineRules[index] = new DfmTextInlineRule();

            var blockRules = BlockRules.ToList();
            index = blockRules.FindLastIndex(s => s is MarkdownNewLineBlockRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownNewLineBlockRule should exist!");
            }
            blockRules.Insert(index + 1, new DfmIncludeBlockRule());
            blockRules.Insert(index + 2, new DfmYamlHeaderBlockRule());
            blockRules.Insert(index + 3, new DfmSectionBlockRule());
            blockRules.Insert(index + 4, new DfmFencesBlockRule());
            blockRules.Insert(index + 5, new DfmNoteBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            if (gfmIndex < 0)
            {
                throw new ArgumentException("GfmParagraphBlockRule should exist!");
            }
            blockRules[gfmIndex] = new DfmParagraphBlockRule();

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            if (markdownBlockQuoteIndex < 0)
            {
                throw new ArgumentException("MarkdownBlockquoteBlockRule should exist!");
            }
            blockRules[markdownBlockQuoteIndex] = new DfmBlockquoteBlockRule();

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();

            Rewriter = InitMarkdownStyle(GetContainer(), baseDir);
        }

        private CompositionHost GetContainer()
        {
            return new ContainerConfiguration()
                .WithAssemblies(
                    from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where !assembly.IsDynamic && !assembly.ReflectionOnly
                    where !assembly.GetName().Name.StartsWith("xunit")
                    select assembly)
                .CreateContainer();
        }

        private static IMarkdownTokenRewriter InitMarkdownStyle(CompositionHost host, string baseDir)
        {
            try
            {
                var builder = new MarkdownValidatorBuilder(host);
                if (!TryLoadValidatorConfig(baseDir, builder))
                {
                    builder.AddValidators(DefaultValidatorName);
                }
                return builder.Create();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.ToString()}");
            }
            return null;
        }

        private static bool TryLoadValidatorConfig(string baseDir, MarkdownValidatorBuilder builder)
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                return false;
            }
            var configFile = Path.Combine(baseDir, MarkdownSytleConfig.MarkdownStyleFileName);
            if (!File.Exists(configFile))
            {
                return false;
            }
            var config = JsonUtility.Deserialize<MarkdownSytleConfig>(configFile);
            if (config.Rules != null &&
                !config.Rules.Any(r => r.RuleName == DefaultValidatorName))
            {
                builder.AddValidators(DefaultValidatorName);
            }
            builder.AddValidators(
                from r in config.Rules ?? new MarkdownValidationRule[0]
                where !r.Disable
                select r.RuleName);
            builder.AddTagValidators(config.TagRules ?? new MarkdownTagValidationRule[0]);
            return true;
        }

        public DfmEngine CreateDfmEngine(object renderer)
        {
            return new DfmEngine(CreateParseContext().SetBaseFolder(_baseDir ?? string.Empty), Rewriter, renderer, Options)
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
