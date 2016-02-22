// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class AzureEngineBuilder : GfmEngineBuilder
    {
        private const string MarkdownExtension = ".md";

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
            blockRules.Insert(index + 3, new AzureSelectorBlockRule());

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

            // Remove GfmUrlInlineRule from inline rules as rewriter can just regards it as plain text
            index = inlineRules.FindLastIndex(s => s is GfmUrlInlineRule);
            inlineRules.RemoveAt(index);
            InlineRules = inlineRules.ToImmutableList();
        }

        protected void CreateRewriters()
        {
            Rewriter = MarkdownTokenRewriterFactory.Composite(
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureNoteBlockToken t) => new DfmNoteBlockToken(t.Rule, t.Context, t.NoteType.Substring("AZURE.".Length), t.Content, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureBlockquoteBlockToken t) => new MarkdownBlockquoteBlockToken(t.Rule, t.Context, t.Tokens, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownLinkInlineToken t) => new MarkdownLinkInlineToken(t.Rule, t.Context, AppendDefaultExtension(t.Href, MarkdownExtension), t.Title, t.Content, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureSelectorBlockToken t) => new DfmSectionBlockToken(t.Rule, t.Context, GenerateAzureSelectorAttributes(t.SelectorType, t.SelectorConditions), t.RawMarkdown)
                        )
                    );
        }

        private string AppendDefaultExtension(string href, string defaultExtension)
        {
            if (PathUtility.IsRelativePath(href) && string.IsNullOrEmpty(Path.GetExtension(href)))
            {
                return $"{href}{defaultExtension}";
            }
            return href;
        }

        private string GenerateAzureSelectorAttributes(string selectorType, string selectorConditions)
        {
            StringBuilder sb = new StringBuilder();
            if (string.Equals(selectorType, "AZURE.SELECTOR", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("class=\"op_single_selector\"");
            }
            else if (string.Equals(selectorType, "AZURE.SELECTOR-LIST", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("class=\"op_multi_selector\"");
                if (!string.IsNullOrEmpty(selectorConditions))
                {
                    var conditions = selectorConditions.Split('|').Select(c => c.Trim());
                    int i = 0;
                    foreach(var condition in conditions)
                    {
                        sb.Append($" title{++i}=\"{HttpUtility.HtmlEncode(condition)}\"");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
