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
                new IMarkdownRule[]
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
                            (IMarkdownRewriteEngine e, MarkdownLinkInlineToken t) => new MarkdownLinkInlineToken(t.Rule, t.Context, NormalizeAzureLink(t.Href, MarkdownExtension, t.Context, t.SourceInfo.Markdown, t.SourceInfo.LineNumber.ToString()), t.Title, t.Content, t.SourceInfo, t.LinkType, t.RefId)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownImageInlineToken t) => new MarkdownImageInlineToken(t.Rule, t.Context, CheckNonMdRelativeFileHref(t.Href, t.Context, t.SourceInfo.Markdown, t.SourceInfo.LineNumber.ToString()), t.Title, t.Text, t.SourceInfo, t.LinkType, t.RefId)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureSelectorBlockToken t) => new DfmSectionBlockToken(t.Rule, t.Context, GenerateAzureSelectorAttributes(t.SelectorType, t.SelectorConditions), t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureHtmlMetadataBlockToken t) => new DfmYamlHeaderBlockToken(t.Rule, t.Context, GenerateYamlHeaderContent(t.Properties, t.Tags), t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureMigrationIncludeBlockToken t) => new DfmIncludeBlockToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureMigrationIncludeInlineToken t) => new DfmIncludeInlineToken(t.Rule, t.Context, t.Src, t.Name, t.Title, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureVideoBlockToken t) => new DfmVideoBlockToken(t.Rule, t.Context, GenerateAzureVideoLink(t.Context, t.VideoId, t.SourceInfo.Markdown, t.SourceInfo.LineNumber.ToString()), t.SourceInfo)
                        )
                    );
        }

        private string NormalizeAzureLink(string href, string defaultExtension, IMarkdownContext context, string rawMarkdown, string line)
        {
            string link = href;

            // link change to the href result after append default extension.
            var result = AppendDefaultExtension(href, defaultExtension, context, rawMarkdown, line);
            link = result.Href;

            // link change if the azure link need to be resolved.
            if (result.NeedContinue && result.NeedResolveAzureLink.HasValue && result.NeedResolveAzureLink == true)
            {
                link = GenerateAzureLinkHref(context, link, rawMarkdown, line);
            }
            return link;
        }

        /// <summary>
        /// Append default extension to href by condition
        /// </summary>
        /// <param name="href">original href string</param>
        /// <param name="defaultExtension">default extension to append</param>
        /// <returns>Href with default extension appended</returns>
        private AppendDefaultExtensionResult AppendDefaultExtension(string href, string defaultExtension, IMarkdownContext context, string rawMarkdown, string line)
        {
            // If the context doesn't have necessary info, return the original href
            if (!context.Variables.ContainsKey("path"))
            {
                return new AppendDefaultExtensionResult(false, href, null);
            }
            var currentFilePath = (string)context.Variables["path"];

            try
            {
                if (!PathUtility.IsRelativePath(href))
                {
                    return new AppendDefaultExtensionResult(false, href, null);
                }
            }
            catch (ArgumentException)
            {
                Logger.LogWarning($"Invalid reference {href} in file: {currentFilePath}. Raw: {rawMarkdown}", null, currentFilePath, line);
                return new AppendDefaultExtensionResult(false, href, null);
            }

            var index = href.IndexOf('#');
            if (index == -1)
            {
                href = href.TrimEnd('/');
                var extension = Path.GetExtension(href);

                // Regard all the relative path with no extension as markdown file that missing .md
                if (string.IsNullOrEmpty(extension))
                {
                    return new AppendDefaultExtensionResult(true, $"{href}{defaultExtension}", true);
                }
                else
                {
                    bool isMarkdownFile = extension.Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase);
                    return new AppendDefaultExtensionResult(true, href, isMarkdownFile);
                }
            }
            else if (index == 0)
            {
                return new AppendDefaultExtensionResult(true, href, false);
            }
            else
            {
                var hrefWithoutAnchor = href.Remove(index).TrimEnd('/');
                var anchor = href.Substring(index);
                var extension = Path.GetExtension(hrefWithoutAnchor);
                if (string.IsNullOrEmpty(extension))
                {
                    return new AppendDefaultExtensionResult(true, $"{hrefWithoutAnchor}{defaultExtension}{anchor}", true);
                }
                else
                {
                    bool isMarkdownFile = extension.Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase);
                    return new AppendDefaultExtensionResult(true, $"{hrefWithoutAnchor}{anchor}", isMarkdownFile);
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

        private string GenerateAzureLinkHref(IMarkdownContext context, string href, string rawMarkdown, string line)
        {
            if (string.IsNullOrEmpty(href))
            {
                return string.Empty;
            }

            StringBuffer content = StringBuffer.Empty;

            // If the context doesn't have necessary info, return the original href
            if (!context.Variables.ContainsKey("path") || !context.Variables.ContainsKey("azureMarkdownFileInfoMapping"))
            {
                return href;
            }

            // if the href is not relative path, return it. Add try catch to keep this method safe.
            try
            {
                if (!PathUtility.IsRelativePath(href))
                {
                    return href;
                }
            }
            catch (ArgumentException)
            {
                Logger.LogWarning($"Invalid reference {href} in file: {(string)context.Variables["path"]}. Raw: {rawMarkdown}", null, (string)context.Variables["path"], line);
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
                Logger.LogWarning($"Can't find markdown reference: {href}. Raw: {rawMarkdown}.", null, currentFilePath, line);
                return href;
            }

            string azureHref = null;
            var hrefFileInfo = azureMarkdownFileInfoMapping[hrefFileName];
            azureHref = string.Format("{0}{1}", PathUtility.MakeRelativePath(Path.GetDirectoryName(currentFilePath), hrefFileInfo.FilePath), anchor);

            return azureHref;
        }

        private string CheckNonMdRelativeFileHref(string nonMdHref, IMarkdownContext context, string rawMarkdown, string line)
        {
            // If the context doesn't have necessary info or nonMdHref is not a relative path, return the original href
            if (!context.Variables.ContainsKey("path") || !PathUtility.IsRelativePath(nonMdHref))
            {
                return nonMdHref;
            }

            var currentFilePath = (string)context.Variables["path"];
            var currentFolderPath = Path.GetDirectoryName(currentFilePath);

            var nonMdExpectedPath = Path.Combine(currentFolderPath, nonMdHref);
            if (!File.Exists(nonMdExpectedPath))
            {
                Logger.LogWarning($"Can't find resource reference: {nonMdHref}. Raw: {rawMarkdown}.", null, currentFilePath, line);
            }
            return nonMdHref;
        }


        private string GenerateAzureVideoLink(IMarkdownContext context, string azureVideoId, string rawMarkdown, string line)
        {
            if (!context.Variables.TryGetValue("path", out object path))
            {
                Logger.LogWarning("Can't get current file path. Skip video token rewriter.");
                return azureVideoId;
            }

            if (!context.Variables.ContainsKey("azureVideoInfoMapping"))
            {
                Logger.LogWarning($"Can't find the whole azure video info mapping. Raw: {rawMarkdown}", null, path.ToString(), line);
                return azureVideoId;
            }

            var azureVideoInfoMapping = (IReadOnlyDictionary<string, AzureVideoInfo>)context.Variables["azureVideoInfoMapping"];
            if (azureVideoInfoMapping == null || !azureVideoInfoMapping.ContainsKey(azureVideoId))
            {
                Logger.LogWarning($"Can't find video reference: {azureVideoId}. Raw: {rawMarkdown}", null, path.ToString(), line);
                return azureVideoId;
            }

            var azureVideoInfo = azureVideoInfoMapping[azureVideoId];
            return azureVideoInfo.Link;
        }
    }

    internal class AppendDefaultExtensionResult
    {
        public AppendDefaultExtensionResult(bool needContinue, string href, bool? needResolveAzureLink)
        {
            NeedContinue = needContinue;
            Href = href;
            NeedResolveAzureLink = needResolveAzureLink;
        }

        /// <summary>
        /// Indicate whether need to continue to do the following transform for the href
        /// </summary>
        public bool NeedContinue { get; }

        /// <summary>
        /// Href after processing append default extension method
        /// </summary>
        public string Href { get; }

        /// <summary>
        /// True if it is a markdown file. Otherwise false. Null when the NeedContinue is false.
        /// </summary>
        public bool? NeedResolveAzureLink { get; }
    }
}
