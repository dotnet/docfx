// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using CsQuery;

    public class DocfxProcessor
    {
        public static string DocfxProcess(string markdownContent)
        {
            string baseDir = Console.ReadLine();
            string relativePath = Console.ReadLine();
            string isFirstTime = Console.ReadLine();
            string result;
            if (!string.IsNullOrEmpty(isFirstTime) && isFirstTime.ToLower() == "true")
            {
                string previewFilePath = new Uri(Console.ReadLine()).LocalPath;
                string pageUpdateJsFilePath = new Uri(Console.ReadLine()).LocalPath;
                result = DocfxProcessCore(baseDir, relativePath, markdownContent, true, previewFilePath,
                    pageUpdateJsFilePath);
            }
            else
            {
                result = DocfxProcessCore(baseDir, relativePath, markdownContent);
            }

            return result;
        }

        private static string DocfxProcessCore(string baseDir, string relativePath, string markdownContent,
            bool isFirstTime = false, string previewFilePath = null, string pageUpdateJsFilePath = null)
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                throw new DocfxPreviewException("Base directory error");
            }
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new DocfxPreviewException("Relative path error");
            }
            var markupResult = DfmMarkup(baseDir, relativePath, markdownContent);
            if (!isFirstTime)
            {
                return markupResult;
            }

            if (string.IsNullOrEmpty(previewFilePath))
            {
                throw new DocfxPreviewException("Preview file path error");
            }
            if (string.IsNullOrEmpty(pageUpdateJsFilePath))
            {
                throw new DocfxPreviewException("Page Update js file path error");
            }

            PreviewJsonConfig config = PreviewCommand.ParsePreviewCommand(baseDir);

            // Path of Html which generated from target markdown file
            string targetHtmlPath = Path.Combine(Path.GetDirectoryName(previewFilePath),
                Path.GetFileNameWithoutExtension(relativePath) + ".html");

            if (string.IsNullOrEmpty(targetHtmlPath))
            {
                // TODO: If the return value is not a complete Html, it should be contacted with an Html header and tail
                return markupResult;
            }

            string htmlString = File.ReadAllText(targetHtmlPath);

            CQ dom = htmlString;

            CQ addElements = $"<script type='text/javascript' src='{pageUpdateJsFilePath}'></script>" +
                             $"<meta name='pageRefreshFunctionName' content ='{config.PageRefreshFunctionName}'>" +
                             $"<meta name='port' content='{config.Port}'>" +
                             $"<meta name='filePath' content='{relativePath}'>" +
                             $"<meta name='markupTagType' content='{config.MarkupTagType}'>" +
                             $"<meta name='markupClassName' content='{config.MarkupClassName}'>";

            // Append page update js
            dom.Select("script")
                .Last()
                .After(
                    addElements.Elements.First());

            for (int i = 1; i < addElements.Length; i++)
            {
                dom.Select("meta")
                    .Last().
                    After(addElements.Elements.ElementAt(i));
            }

            // Replace 'https' to 'http'
            // Replace reference relative path to absolute path
            foreach (var item in config.References)
            {
                dom.Select(item.Key).Each((i, e) =>
                {
                    var path = e.GetAttribute(item.Value);
                    if (string.IsNullOrEmpty(path))
                        return;
                    if (path.StartsWith("https"))
                    {
                        e.SetAttribute(item.Value, path.Replace("https", "http"));
                        return;
                    }

                    e.SetAttribute(item.Value, GetAbsolutePath(targetHtmlPath, path));
                });
            }

            // For Docs
            // Replace toc relative path to absolute path
            dom.Select("meta").Each((i, e) =>
            {
                var metaName = e.GetAttribute("name");
                if (metaName == config.TocMetadataName)
                    e.SetAttribute("content", GetAbsolutePath(targetHtmlPath, e.GetAttribute("content")));
            });

            File.WriteAllText(previewFilePath, dom.Render());
            return markupResult;
        }

        private static string DfmMarkup(string baseDir, string filename, string markdownContent)
        {
            // TODO: different editor use different child process so there is no need to create dfm service each time
            DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
            IMarkdownService dfmService =
                dfmServiceProvider.CreateMarkdownService(new MarkdownServiceParameters {BasePath = baseDir});
            return dfmService.Markup(markdownContent, filename).Html;
        }

        private static void DocfxRebuild()
        {
            // TODO: Implement docfx rebuild
            throw new NotImplementedException();
        }

        private static string GetAbsolutePath(string originHtmlPath, string elementRelativePath)
        {
            string rawAbsolutePath = new Uri(new Uri(PreviewConstants.PathPrefix + originHtmlPath), elementRelativePath).AbsoluteUri;
            return rawAbsolutePath.Substring(PreviewConstants.PathPrefix.Length);
        }
    }
}
