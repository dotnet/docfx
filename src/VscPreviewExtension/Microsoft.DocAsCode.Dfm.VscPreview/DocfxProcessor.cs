// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using CsQuery;

    public class DocfxProcessor
    {
        public static string DocfxProcess(string baseDir, string relativePath, string markdownContent)
        {
            PreviewJsonConfig config = PreviewCommand.ParsePreviewCommand(baseDir);

            var markupResult = DfmMarkup(baseDir, relativePath, markdownContent.ToString());

            string originHtmlPath = FindOriginHtml(baseDir, relativePath, config.OutputFolder);

            if (string.IsNullOrEmpty(originHtmlPath))
            {
                // TODO: If the return value is not a complete Html, it should be contacted with an Html header and tail
                return markupResult;
            }

            string htmlString = File.ReadAllText(originHtmlPath);

            CQ dom = htmlString;

            // Update markup result
            dom.Select(config.MarkupResultLocation).Html(markupResult);

            foreach (var item in config.References)
            {
                dom.Select(item.Key).Each((i, e) =>
                {
                    var path = e.GetAttribute(item.Value);
                    e.SetAttribute(item.Value, GetAbsolutePath(originHtmlPath, path));
                });
            }

            return dom.Render();
        }

        private static string DfmMarkup(string baseDir, string filename, string markdownContent)
        {
            // TODO: different editor use different child process so there is no need to create dfm service each time
            DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
            IMarkdownService dfmService =
                dfmServiceProvider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = baseDir });
            return dfmService.Markup(markdownContent, filename).Html;
        }

        private static string FindOriginHtml(string baseDir, string relativePath, string outPutFolder)
        {
            string originHtmlPath = Path.Combine(baseDir, outPutFolder,
                Path.GetDirectoryName(relativePath), Path.GetFileNameWithoutExtension(relativePath) + ".html");
            if (!File.Exists(originHtmlPath))
            {
                // Rerun Docfx
                DocfxRebuild();
                return string.Empty;
            }
            return originHtmlPath;
        }

        private static void DocfxRebuild()
        {
            // TODO: Docfx rebuild
            throw new DocfxPreviewException("Docfx rebuild is not supported now");
        }

        private static string GetAbsolutePath(string originHtmlPath, string elementRelativePath)
        {
            string rawAbsolutePath = new Uri(new Uri(PreviewConstants.PathPrefix + originHtmlPath), elementRelativePath).AbsoluteUri;
            return rawAbsolutePath.Substring(PreviewConstants.PathPrefix.Length);
        }
    }
}
