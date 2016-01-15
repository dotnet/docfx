// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Immutable;
    using System.Composition.Hosting;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmEngineBuilder : GfmEngineBuilder
    {
        public DfmEngineBuilder(Options options) : base(options)
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

            Rewriter = InitMarkdownStyle(GetContainer());
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

        private static IMarkdownTokenRewriter InitMarkdownStyle(CompositionHost host)
        {
            try
            {
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    var config = JsonUtility.Deserialize<MarkdownSytleConfig>(MarkdownSytleConfig.MarkdownStyleFileName);
                    var builder = new MarkdownValidatorBuilder(host);
                    builder.AddValidators(from r in config.Rules where !r.Disable select r.RuleName);
                    builder.AddTagValidators(config.TagRules);
                    return builder.Create();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.ToString()}");
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
