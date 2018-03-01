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

    public class AzureEngineBuilder : GfmEngineBuilder
    {
        private const string MarkdownExtension = ".md";

        private const string ExternalResourceFolderName = "ex_resource";

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
            blockRules.InsertRange(
                index + 1,
                new IMarkdownRule[]
                {
                    new DfmYamlHeaderBlockRule(),
                    new AzureIncludeBlockRule(),
                    new AzureNoteBlockRule(),
                    new AzureSelectorBlockRule()
                });

            index = blockRules.FindLastIndex(s => s is MarkdownHtmlBlockRule);
            if (index < 1)
            {
                throw new ArgumentException($"{nameof(MarkdownHtmlBlockRule)} should exist and shouldn't be the first one rule!");
            }
            blockRules.Insert(index - 1, new AzureHtmlMetadataBlockRule());

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
                            (IMarkdownRewriteEngine e, AzureNoteBlockToken t) => new DfmNoteBlockToken(t.Rule, t.Context, t.NoteType.Substring("AZURE.".Length), t.Content, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureBlockquoteBlockToken t) => new MarkdownBlockquoteBlockToken(t.Rule, t.Context, t.Tokens, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownImageInlineToken t) => new MarkdownImageInlineToken(t.Rule, t.Context, FixNonMdRelativeFileHref(t.Href, t.Context, t.SourceInfo.Markdown), t.Title, t.Text, t.SourceInfo, t.LinkType, t.RefId)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownLinkInlineToken t) => new MarkdownLinkInlineToken(t.Rule, t.Context, NormalizeAzureLink(t.Href, MarkdownExtension, t.Context, t.SourceInfo.Markdown), t.Title, t.Content, t.SourceInfo, t.LinkType, t.RefId)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureSelectorBlockToken t) => new DfmSectionBlockToken(t.Rule, t.Context, GenerateAzureSelectorAttributes(t.SelectorType, t.SelectorConditions), t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, AzureHtmlMetadataBlockToken t) => new DfmYamlHeaderBlockToken(t.Rule, t.Context, GenerateYamlHeaderContent(t.Properties, t.Tags), t.SourceInfo)
                        )
                    );
        }

        private string NormalizeAzureLink(string href, string defaultExtension, IMarkdownContext context, string rawMarkdown)
        {
            var link = AppendDefaultExtension(href, defaultExtension, out bool isHrefRelativeNonMdFile);
            if (isHrefRelativeNonMdFile)
            {
                link = FixNonMdRelativeFileHref(link, context, rawMarkdown);
            }
            else
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

        private string FixNonMdRelativeFileHref(string nonMdHref, IMarkdownContext context, string rawMarkdown)
        {
            // If the context doesn't have necessary info or nonMdHref is not a relative path, return the original href
            if (!context.Variables.ContainsKey("path") || !PathUtility.IsRelativePath(nonMdHref))
            {
                return nonMdHref;
            }

            var currentFilePath = (string)context.Variables["path"];
            var currentFolderPath = Path.GetDirectoryName(currentFilePath);

            try
            {
                // if the relative path (not from azure resource file info mapping) is under docset. Just return it.
                var nonMdHrefFullPath = Path.GetFullPath(Path.Combine(currentFolderPath, nonMdHref));
                if (PathUtility.IsPathUnderSpecificFolder(nonMdHrefFullPath, currentFolderPath))
                {
                    return nonMdHref;
                }
                else
                {
                    Logger.LogVerbose($"Relative path:{nonMdHref} is not under {currentFolderPath} of file {currentFilePath}. Use ex_resource to replace the link.");
                }

                // if azure resource file info doesn't exist, log warning and return
                if (!context.Variables.ContainsKey("azureResourceFileInfoMapping"))
                {
                    Logger.LogWarning($"Can't find azure resource file info mapping. Couldn't fix href: {nonMdHref} in file {currentFilePath}. raw: {rawMarkdown}");
                    return nonMdHref;
                }

                var nonMdHrefFileName = Path.GetFileName(nonMdHref);
                var azureResourceFileInfoMapping = (Dictionary<string, AzureFileInfo>)context.Variables["azureResourceFileInfoMapping"];
                if (!azureResourceFileInfoMapping.TryGetValue(nonMdHrefFileName, out AzureFileInfo azureResourceFileInfo))
                {
                    Logger.LogWarning($"Can't find info for file name {nonMdHrefFileName} in azure resource file info mapping. Couldn't fix href: {nonMdHref} in file {currentFilePath}. raw: {rawMarkdown}");
                    return nonMdHref;
                }

                // If the nonMdHref is under same docset with current file. No need to fix that.
                if (PathUtility.IsPathUnderSpecificFolder(azureResourceFileInfo.FilePath, currentFolderPath))
                {
                    return nonMdHref;
                }

                // If the nonMdHref is under different docset with current file but not exists. Then log warning and won't fix.
                if (!File.Exists(azureResourceFileInfo.FilePath))
                {
                    Logger.LogWarning($"{nonMdHref} refer by {currentFilePath} doesn't exists. Won't do link fix. raw: {rawMarkdown}");
                    return nonMdHref;
                }

                // If the nonMdHref is under different docset with current file and also exists, then fix the link.
                // 1. copy the external file to ex_resource folder. 2. Return new href path to the file under external folder
                var exResourceDir = Directory.CreateDirectory(Path.Combine(currentFolderPath, ExternalResourceFolderName));
                var resDestPath = Path.Combine(exResourceDir.FullName, Path.GetFileName(azureResourceFileInfo.FilePath));
                File.Copy(azureResourceFileInfo.FilePath, resDestPath, true);
                return PathUtility.MakeRelativePath(currentFolderPath, resDestPath);
            }
            catch (NotSupportedException nse)
            {
                Logger.LogWarning($"Warning: FixNonMdRelativeFileHref can't be apply on reference: {nonMdHref}. Exception: {nse.Message}");
                return nonMdHref;
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
                    // If the file is in different docset, then append the absolute path prefix. .html should be remove as docs also don't need it now.
                    azureHref = $"{hrefFileInfo.UriPrefix}/{Path.GetFileNameWithoutExtension(hrefFileName)}{anchor}";
                }
            }

            return azureHref;
        }
    }
}
