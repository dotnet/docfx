// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.IO;
    using CsQuery;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    public class DocfxProcessor : PreviewProcessor
    {
        public static string DocfxProcess()
        {
            string baseDir = Console.ReadLine();
            string relativePath = Console.ReadLine();
            string markdownContent = GetMarkdownContent();
            var result = DocfxProcessCore(baseDir, relativePath, markdownContent.ToString());

            return result;
        }

        private static string DfmMarkup(string basedir, string filename, string markdownContent)
        {
            // TODO: different editor use different child process so there is no need to create dfm service each time
            DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
            IMarkdownService dfmService =
                dfmServiceProvider.CreateMarkdownService(new MarkdownServiceParameters {BasePath = basedir});
            return dfmService.Markup(markdownContent, filename).Html;
        }

        private static string DocfxProcessCore(string basedir, string relativePath, string markdownContent)
        {
            PreviewJsonConfig config = new PreviewJsonConfig();
            config = PreviewCommand.ParsePreviewCommand(basedir);

            var markUpResult = DfmMarkup(basedir, relativePath, markdownContent.ToString());

            string originHtmlPath = FindOriginHtml(basedir, relativePath, config.OutputFolder);

            if (string.IsNullOrEmpty(originHtmlPath))
            {
                // TODO: If the return value is not a complete Html, it should be contacted with an Html header and tail
                return markUpResult;
            }

            string htmlString = File.ReadAllText(originHtmlPath);

            CQ dom = htmlString;

            // Update markUp result
            dom.Select(config.MarkupResultLocation).Html(markUpResult);

            foreach (var item in config.References)
            {
                dom.Select(item.Key).Each((i, e) =>
                {
                    var path = e.GetAttribute(item.Value);
                    e.SetAttribute(item.Value, GetAbusolutePath(originHtmlPath, path, config.Port));
                });
            }

            string html = dom.Render();

            return html;
        }

        private static string FindOriginHtml(string basedir, string relativePath, string outPutFolder)
        {
            string originHtmlPath = Path.Combine(basedir, outPutFolder,
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
        }

        private static string GetAbusolutePath(string originHtmlPath, string elementRelativePath, string port)
        {
            string rawAbusolutePath = new Uri(new Uri(PreviewConstants.PathPrefix + originHtmlPath), elementRelativePath).AbsoluteUri;
            return rawAbusolutePath.Substring(PreviewConstants.PathPrefix.Length).Replace("/", "\\");
        }
    }
}
