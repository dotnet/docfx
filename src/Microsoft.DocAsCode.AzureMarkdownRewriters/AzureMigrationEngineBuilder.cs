﻿// Copyright (c) Microsoft. All rights reserved.
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

    public class AzureMigrationEngineBuilder : GfmEngineBuilder
    {
        private const string MarkdownExtension = ".md";

        public AzureMigrationEngineBuilder(Options options) : base(options)
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
            blockRules.InsertRange(
                index + 1,
                new IMarkdownRule []
                {
                    new DfmYamlHeaderBlockRule(),
                    new AzureMigrationIncludeBlockRule(),
                    new AzureMigrationVideoBlockRule(),
                    new AzureNoteBlockRule(),
                    new AzureSelectorBlockRule()
                });

            index = blockRules.FindLastIndex(s => s is MarkdownHtmlBlockRule);
            if (index < 1)
            {
                throw new ArgumentException($"{nameof(MarkdownHtmlBlockRule)} should exist and shouldn't be the first one rule!");
            }
            blockRules.Insert(index - 1, new AzureMigrationHtmlMetadataBlockRule());

            var gfmIndex = blockRules.FindIndex(item => item is GfmParagraphBlockRule);
            blockRules[gfmIndex] = new AzureMigrationParagraphBlockRule();

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
            inlineRules.Insert(index, new AzureMigrationIncludeInlineRule());

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
                            (IMarkdownRewriteEngine e, AzureNoteBlockToken t) => new DfmNoteBlockToken(t.Rule, t.Context, t.NoteType.Substring("AZURE.".Length), t.Content, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureBlockquoteBlockToken t) => new MarkdownBlockquoteBlockToken(t.Rule, t.Context, t.Tokens, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownLinkInlineToken t) => new MarkdownLinkInlineToken(t.Rule, t.Context, NormalizeAzureLink(t.Href, MarkdownExtension, t.Context, t.SourceInfo.Markdown), t.Title, t.Content, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureSelectorBlockToken t) => new DfmSectionBlockToken(t.Rule, t.Context, GenerateAzureSelectorAttributes(t.SelectorType, t.SelectorConditions), t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureHtmlMetadataBlockToken t) => new DfmYamlHeaderBlockToken(t.Rule, t.Context, GenerateYamlHeaderContent(t.Properties, t.Tags), t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureMigrationIncludeBlockToken t) => new DfmIncludeBlockToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.SourceInfo.Markdown, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureMigrationIncludeInlineToken t) => new DfmIncludeInlineToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.SourceInfo.Markdown, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureVideoBlockToken t) => new DfmVideoBlockToken(t.Rule, t.Context, GenerateAzureVideoLink(t.Context, t.VideoId, t.SourceInfo.Markdown), t.SourceInfo)
                        )
                    );
        }

        private string NormalizeAzureLink(string href, string defaultExtension, IMarkdownContext context, string rawMarkdown)
        {
            bool isHrefRelativeNonMdFile;
            var link = AppendDefaultExtension(href, defaultExtension, out isHrefRelativeNonMdFile);
            if (!isHrefRelativeNonMdFile)
            {
                link = GenerateAzureLinkHref(context, link, rawMarkdown);
            }
            return link;
        }

        /// <summary>
        /// Append default extension to href by condition
        /// </summary>
        /// <param name="href">original href string</param>
        /// <param name="defaultExtension">default extension to append</param>
        /// <param name="isHrefRelativeNonMdFile">true if it is a relative path and not a markdown file. Otherwise false</param>
        /// <returns>Href with default extension appended</returns>
        private string AppendDefaultExtension(string href, string defaultExtension, out bool isHrefRelativeNonMdFile)
        {
            isHrefRelativeNonMdFile = false;
            if (!PathUtility.IsRelativePath(href))
            {
                return href;
            }

            var index = href.IndexOf('#');
            if (index == -1)
            {
                href = href.TrimEnd('/');
                var extension = Path.GetExtension(href);

                // Regard all the relative path with no extension as markdown file that missing .md
                if (string.IsNullOrEmpty(extension))
                {
                    return $"{href}{defaultExtension}";
                }
                else
                {
                    if (!extension.Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        isHrefRelativeNonMdFile = true;
                    }
                    return href;
                }
            }
            else if (index == 0)
            {
                return href;
            }
            else
            {
                var hrefWithoutAnchor = href.Remove(index).TrimEnd('/');
                var anchor = href.Substring(index);
                var extension = Path.GetExtension(hrefWithoutAnchor);
                if (string.IsNullOrEmpty(extension))
                {
                    return $"{hrefWithoutAnchor}{defaultExtension}{anchor}";
                }
                else
                {
                    if (!extension.Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        isHrefRelativeNonMdFile = true;
                    }
                    return $"{hrefWithoutAnchor}{anchor}";
                }
            }
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
                    foreach (var condition in conditions)
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

            // If the context doesn't have necessary info, return the original href
            if (!context.Variables.ContainsKey("path") || !context.Variables.ContainsKey("azureMarkdownFileInfoMapping"))
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
            var azureMarkdownFileInfoMapping = (IReadOnlyDictionary<string, AzureFileInfo>)context.Variables["azureMarkdownFileInfoMapping"];
            if (azureMarkdownFileInfoMapping == null || !azureMarkdownFileInfoMapping.ContainsKey(hrefFileName))
            {
                Logger.LogWarning($"Can't fild reference file: {href} in azure file system for file {currentFilePath}. Raw: {rawMarkdown}");
                return href;
            }

            string azureHref = null;
            var hrefFileInfo = azureMarkdownFileInfoMapping[hrefFileName];
            azureHref = string.Format("{0}{1}", PathUtility.MakeRelativePath(Path.GetDirectoryName(currentFilePath), hrefFileInfo.FilePath), anchor);

            return azureHref;
        }

        private string GenerateAzureVideoLink(IMarkdownContext context, string azureVideoId, string rawMarkdown)
        {
            object path;
            if (!context.Variables.TryGetValue("path", out path))
            {
                Logger.LogWarning("Can't get current file path. Skip video token rewriter.");
                return azureVideoId;
            }

            if (!context.Variables.ContainsKey("azureVideoInfoMapping"))
            {
                Logger.LogWarning($"Can't fild the whole azure video info mapping. Current processing file: {path}, Raw: {rawMarkdown}");
                return azureVideoId;
            }

            var azureVideoInfoMapping = (IReadOnlyDictionary<string, AzureVideoInfo>)context.Variables["azureVideoInfoMapping"];
            if (azureVideoInfoMapping == null || !azureVideoInfoMapping.ContainsKey(azureVideoId))
            {
                Logger.LogWarning($"Can't fild azure video info mapping for file: {path}. Raw: {rawMarkdown}");
                return azureVideoId;
            }

            var azureVideoInfo = azureVideoInfoMapping[azureVideoId];
            return azureVideoInfo.Link;
        }
    }
}
