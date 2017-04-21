// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Collections.Generic;
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
        private readonly DfmServiceProvider _provider = new DfmServiceProvider();

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
                    var content = Preview(context.Message.WorkspacePath, context.Message.RelativePath,
                        context.Message.MarkdownContent, context.Message.IfSeparateMarkupResult,
                        context.Message.IfWriteTempPreviewFile, context.Message.TempPreviewFilePath,
                        context.Message.PageRefreshJsFilePath, context.Message.OriginalHtmlPath);
                    Utility.ReplySuccessfulResponse(context.HttpContext, content,
                        context.Message.IfSeparateMarkupResult ? ContentType.Json : ContentType.Html);
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

        private string Preview(string workspacePath, string relativePath, string markdownContent, bool ifSeparateMarkupResult = false,
            bool ifWriteTempPreviewFile = false, string tempPreviewFilePath = null, string pageUpdateJsFilePath = null,
            string originalHtmlPath = null)
        {
            if (string.IsNullOrEmpty(workspacePath))
            {
                throw new HandlerClientException("Base directory should not be null or empty");
            }
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new HandlerClientException("Relative path should not be null or empty");
            }
            string result = DfmMarkup(workspacePath, relativePath, markdownContent);
            if (ifSeparateMarkupResult)
            {
                var separatedMarkupResult = new Dictionary<string, string>();
                var htmlInfo = BuildConceptualUtility.SeparateHtml(result);
                separatedMarkupResult["rawTitle"] = htmlInfo.RawTitle;
                separatedMarkupResult["content"] = htmlInfo.Content;
                result = JsonConvert.SerializeObject(separatedMarkupResult);
            }
            if (!ifWriteTempPreviewFile)
            {
                return result;
            }

            if (string.IsNullOrEmpty(originalHtmlPath))
            {
                throw new HandlerClientException("Built Html path should not be null or empty");
            }
            if (string.IsNullOrEmpty(pageUpdateJsFilePath))
            {
                throw new HandlerClientException("Page update js file path should not be null or empty");
            }

            PreviewJsonConfig config = PreviewCommand.ParsePreviewCommand(workspacePath);

            originalHtmlPath = new Uri(originalHtmlPath).LocalPath;
            pageUpdateJsFilePath = new Uri(pageUpdateJsFilePath).LocalPath;
            tempPreviewFilePath = new Uri(tempPreviewFilePath).LocalPath;

            string htmlString = File.ReadAllText(originalHtmlPath);

            CQ dom = htmlString;

            CQ addElements = $"<script type='text/javascript' src='{pageUpdateJsFilePath}'></script>" +
                             $"<meta name='pageRefreshFunctionName' content ='{config.PageRefreshFunctionName}'>" +
                             $"<meta name='port' content='{config.NavigationPort}'>" +
                             $"<meta name='filePath' content='{relativePath}'>" +
                             $"<meta name='markupTagType' content='{config.MarkupTagType}'>" +
                             $"<meta name='markupClassName' content='{config.MarkupClassName}'>";

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

        private string DfmMarkup(string workspacePath, string relativePath, string markdownContent)
        {
            var service = _provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });

            return service.Markup(markdownContent, relativePath).Html;
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
