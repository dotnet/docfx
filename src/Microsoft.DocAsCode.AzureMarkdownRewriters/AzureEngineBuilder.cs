// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class AzureEngineBuilder : GfmEngineBuilder
    {
        private const string MarkdownExtension = ".md";
        private const string HtmlExtension = ".html";

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
            blockRules.Insert(index + 1, new DfmYamlHeaderBlockRule());
            blockRules.Insert(index + 2, new AzureIncludeBlockRule());
            blockRules.Insert(index + 3, new AzureNoteBlockRule());
            blockRules.Insert(index + 4, new AzureSelectorBlockRule());

            index = blockRules.FindLastIndex(s => s is MarkdownHtmlBlockRule);
            if (index < 1)
            {
                throw new ArgumentException($"{nameof(MarkdownHtmlBlockRule)} should exist and shouldn't be the first one rule!");
            }
            blockRules.Insert(index - 1, new AzureHtmlMetadataBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new AzureParagraphBlockRule();

            var markdownBlockQuoteIndex = blockRules.FindIndex(item => item is MarkdownBlockquoteBlockRule);
            blockRules[markdownBlockQuoteIndex] = new AzureBlockquoteBlockRule();
            blockRules.Insert(markdownBlockQuoteIndex, new AzureVideoBlockRule());

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

        public override IMarkdownEngine CreateEngine(object renderer)
        {
            var engine = (MarkdownEngine)base.CreateEngine(renderer);
            engine.MaxExtractCount = 100;
            return engine;
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
                            (IMarkdownRewriteEngine e, MarkdownLinkInlineToken t) => new MarkdownLinkInlineToken(t.Rule, t.Context, NormalizeAzureLink(t.Href, MarkdownExtension, t.Context, t.RawMarkdown), t.Title, t.Content, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureSelectorBlockToken t) => new DfmSectionBlockToken(t.Rule, t.Context, GenerateAzureSelectorAttributes(t.SelectorType, t.SelectorConditions), t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureHtmlMetadataBlockToken t) => new DfmYamlHeaderBlockToken(t.Rule, t.Context, GenerateYamlHeaderContent(t.Properties, t.Tags), t.RawMarkdown)
                        )
                    );
        }

        private string NormalizeAzureLink(string href, string defaultExtension, IMarkdownContext context, string rawMarkdown)
        {
            var link = AppendDefaultExtension(href, defaultExtension);
            link = GenerateAzureLinkHref(context, link, rawMarkdown);
            return link;
        }

        private string AppendDefaultExtension(string href, string defaultExtension)
        {
            if (PathUtility.IsRelativePath(href))
            {
                var index = href.IndexOf('#');
                if (index == -1)
                {
                    if (string.IsNullOrEmpty(Path.GetExtension(href)))
                    {
                        return $"{href}{defaultExtension}";
                    }
                    else
                    {
                        return href;
                    }
                }
                else if (index == 0)
                {
                    return href;
                }
                else
                {
                    var hrefWithoutAnchor = href.Remove(index);
                    var anchor = href.Substring(index);
                    if (string.IsNullOrEmpty(Path.GetExtension(href)))
                    {
                        return $"{hrefWithoutAnchor}{defaultExtension}{anchor}";
                    }
                    else
                    {
                        return href;
                    }
                }
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

        private string GenerateYamlHeaderContent(IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, string> tags)
        {
            var propertiesSw = new StringWriter();
            YamlUtility.Serialize(propertiesSw, properties);
            var tagsSw = new StringWriter();
            YamlUtility.Serialize(tagsSw, tags);
            return MarkdownEngine.Normalize(propertiesSw.ToString() + "\n" + tagsSw.ToString());
        }

        private string GenerateAzureLinkHref(IMarkdownContext context, string href, string rawMarkdown)
        {
            StringBuffer content = StringBuffer.Empty;

            // If the context doesn't have necessary, return the original href
            if (!context.Variables.ContainsKey("path") || !context.Variables.ContainsKey("azureFileInfoMapping"))
            {
                return href;
            }

            // if the href is not relative path, return it
            if (!PathUtility.IsRelativePath(href))
            {
                return href;
            }

            // deal with bookmark. Get file name and anchor
            string hrefFileName = string.Empty;
            string anchor = string.Empty;
            var index = href.IndexOf('#');
            if (index == -1)
            {
                hrefFileName = Path.GetFileName(href);
            }
            else if (index == 0)
            {
                return href;
            }
            else
            {
                hrefFileName = Path.GetFileName(href.Remove(index));
                anchor = href.Substring(index);
            }

            // deal with different kinds of relative paths
            var currentFilePath = (string)context.Variables["path"];
            var azureFileInfoMapping = (IReadOnlyDictionary<string, AzureFileInfo>)context.Variables["azureFileInfoMapping"];
            if (azureFileInfoMapping == null || !azureFileInfoMapping.ContainsKey(hrefFileName))
            {
                Logger.LogWarning($"Can't fild reference file: {href} in azure file system for file {currentFilePath}. Raw: {rawMarkdown}");
                return href;
            }

            string azureHref = null;
            var hrefFileInfo = azureFileInfoMapping[hrefFileName];

            // Not in docsets and transform to azure external link
            if (hrefFileInfo.NeedTransformToAzureExternalLink)
            {
                azureHref = $"{hrefFileInfo.UriPrefix}/{Path.GetFileNameWithoutExtension(hrefFileName)}{anchor}";
            }
            else
            {
                var hrefPath = hrefFileInfo.FilePath;

                // It is correct for Azure strucuture. Azure articles are all under same folder
                var isHrefInsameDocset = PathUtility.IsPathUnderSpecificFolder(hrefPath, Path.GetDirectoryName(currentFilePath));

                // In same docset with current file, use relative path. Otherwise, use docset link prefix
                if (isHrefInsameDocset)
                {
                    azureHref = string.Format("{0}{1}", PathUtility.MakeRelativePath(Path.GetDirectoryName(currentFilePath), hrefFileInfo.FilePath), anchor);
                }
                else
                {
                    azureHref = $"{hrefFileInfo.UriPrefix}/{Path.GetFileNameWithoutExtension(hrefFileName)}{HtmlExtension}{anchor}";
                }
            }

            return azureHref;
        }
    }
}
