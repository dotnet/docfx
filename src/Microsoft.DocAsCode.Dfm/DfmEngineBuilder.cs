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
        private readonly string _baseDir;

        public DfmEngineBuilder(Options options, string baseDir = null, string templateDir = null) : base(options)
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

            Rewriter = InitMarkdownStyle(GetContainer(), baseDir, templateDir);
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

        private static IMarkdownTokenRewriter InitMarkdownStyle(CompositionHost host, string baseDir, string templateDir)
        {
            try
            {
                var builder = new MarkdownValidatorBuilder(host);
                LoadValidatorConfig(baseDir, templateDir, builder);
                return builder.Create();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.ToString()}");
            }
            return null;
        }

        private static void LoadValidatorConfig(string baseDir, string templateDir, MarkdownValidatorBuilder builder)
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                return;
            }
            if (templateDir != null)
            {
                var configFolder = Path.Combine(templateDir, MarkdownSytleDefinition.MarkdownStyleDefinitionFolderName);
                if (Directory.Exists(configFolder))
                {
                    LoadValidatorDefinition(configFolder, builder);
                }
            }
            var configFile = Path.Combine(baseDir, MarkdownSytleConfig.MarkdownStyleFileName);
            if (File.Exists(configFile))
            {
                var config = JsonUtility.Deserialize<MarkdownSytleConfig>(configFile);
                builder.AddValidators(config.Rules);
                builder.AddTagValidators(config.TagRules);
            }
            builder.EnsureDefaultValidator();
        }

        private static void LoadValidatorDefinition(string mdStyleDefPath, MarkdownValidatorBuilder builder)
        {
            if (Directory.Exists(mdStyleDefPath))
            {
                foreach (var configFile in Directory.GetFiles(mdStyleDefPath, "*" + MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix))
                {
                    var fileName = Path.GetFileName(configFile);
                    var category = fileName.Remove(fileName.Length - MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix.Length);
                    var config = JsonUtility.Deserialize<MarkdownSytleDefinition>(configFile);
                    builder.AddTagValidators(category, config.TagRules);
                    builder.AddValidators(config.Rules);
                }
            }
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
