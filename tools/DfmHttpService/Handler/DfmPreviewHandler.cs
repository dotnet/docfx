// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using CsQuery;
    using Newtonsoft.Json;

    internal class DfmPreviewHandler : IHttpHandler
    {
        private readonly IMarkdownService _service;

        public DfmPreviewHandler(string workspacePath, bool isDfmLatest)
        {
            DfmServiceProvider provider = isDfmLatest ? new DfmServiceProvider() : new DfmLegacyServiceProvider();

            _service = provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });
        }

        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.Preview;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                try
                {
                    var content = Preview(context.Message);
                    Utility.ReplySuccessfulResponse(context.HttpContext, content,
                        context.Message.ShouldSeparateMarkupResult ? ContentType.Json : ContentType.Html);
                }
                catch(HandlerClientException ex)
                {
                    Utility.ReplyClientErrorResponse(context.HttpContext, ex.Message);
                }
                catch (Exception ex)
                {
                    Utility.ReplyServerErrorResponse(context.HttpContext, ex.Message);
                }
            });
        }

        private string Preview(CommandMessage contextMessage)
        {
            if (string.IsNullOrEmpty(contextMessage.RelativePath))
            {
                throw new HandlerClientException("Relative path should not be null or empty");
            }
            string result = DfmMarkup(contextMessage.RelativePath, contextMessage.MarkdownContent);
            if (contextMessage.ShouldSeparateMarkupResult)
            {
                var htmlInfo = HtmlDocumentUtility.SeparateHtml(result);
                var separatedMarkupResult =
                    new {rawTitle = htmlInfo.RawTitle, contentWithoutRawTitle = htmlInfo.Content};
                result = JsonConvert.SerializeObject(separatedMarkupResult);
            }
            if (string.IsNullOrEmpty(contextMessage.TempPreviewFilePath))
            {
                return result;
            }

            // TODO: move this part to client
            if (string.IsNullOrEmpty(contextMessage.OriginalHtmlPath))
            {
                throw new HandlerClientException("Built Html path should not be null or empty");
            }
            if (string.IsNullOrEmpty(contextMessage.PageRefreshJsFilePath))
            {
                throw new HandlerClientException("Page update js file path should not be null or empty");
            }

            PreviewJsonConfig config = PreviewCommand.ParsePreviewCommand(contextMessage.WorkspacePath);

            var originalHtmlPath = new Uri(contextMessage.OriginalHtmlPath).LocalPath;
            var pageRefreshJsFilePath = new Uri(contextMessage.PageRefreshJsFilePath).LocalPath;
            var tempPreviewFilePath = new Uri(contextMessage.TempPreviewFilePath).LocalPath;

            string htmlString = File.ReadAllText(originalHtmlPath);

            CQ dom = htmlString;

            CQ addElements = $"<script type='text/javascript' src='{pageRefreshJsFilePath}'></script>" +
                             $"<meta name='port' content='{contextMessage.NavigationPort}'>" +
                             $"<meta name='filePath' content='{contextMessage.RelativePath}'>";

            foreach (var addElement in addElements)
            {
                dom.Select(addElement.NodeName)
                    .Last()
                    .After(addElement);
            }

            // Replace 'https' to 'http' for that VS Code don't support reference which use https protocol now
            // Replace reference relative path to absolute path
            foreach (var item in config.References)
            {
                dom.Select(item.Key).Each((i, e) =>
                {
                    var path = e.GetAttribute(item.Value);
                    if (string.IsNullOrEmpty(path))
                        return;
                    // VSCode bug: https://github.com/Microsoft/vscode/issues/23020
                    // Remove when bug fixed
                    if (path.StartsWith("http"))
                    {
                        if (path.StartsWith("https"))
                        {
                            e.SetAttribute(item.Value, ReplaceFirstOccurrence(path, "https", "http"));
                        }
                        return;
                    }
                    if (Path.IsPathRooted(path))
                    {
                        e.SetAttribute(item.Value, path);
                    }
                    else
                    {
                        e.SetAttribute(item.Value, GetAbsolutePath(originalHtmlPath, path));
                    }
                });
            }

            // For Docs
            // Replace toc relative path to absolute path
            dom.Select("meta").Each((i, e) =>
            {
                // TODO: Implement breadcrumb
                var metaName = e.GetAttribute("name");
                if (metaName == config.TocMetadataName)
                {
                    e.SetAttribute("content", GetAbsolutePath(originalHtmlPath, e.GetAttribute("content")));
                }
            });

            File.WriteAllText(tempPreviewFilePath, dom.Render());
            return result;
        }

        private string DfmMarkup(string relativePath, string markdownContent)
        {
            return _service.Markup(markdownContent, relativePath).Html;
        }

        private string GetAbsolutePath(string builtHtmlPath, string elementRelativePath)
        {
            string rawAbsolutePath = new Uri(new Uri(PreviewConstants.PathPrefix + builtHtmlPath), elementRelativePath).AbsoluteUri;
            return rawAbsolutePath.Substring(PreviewConstants.PathPrefix.Length);
        }

        private string ReplaceFirstOccurrence(string input, string oldValue, string newValue)
        {
            Regex rgx = new Regex(oldValue);
            return rgx.Replace(input, newValue, 1);
        }
    }
}
