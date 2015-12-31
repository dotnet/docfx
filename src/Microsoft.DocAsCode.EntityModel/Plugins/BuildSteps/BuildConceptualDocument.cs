// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildConceptualDocument : BaseDocumentBuildStep
    {
        private const string ConceputalKey = "conceptual";
        private const string DocumentTypeKey = "documentType";

        public override string Name => nameof(BuildConceptualDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content[ConceputalKey];
            var result = host.Markup(markdown, model.FileAndType);
            content[ConceputalKey] = result.Html;

            var contentTitles = ExtractContentTitlesFromHtml(result.Html);
            content["title"] = contentTitles.Title;
            content["articleTitleHtml"] = contentTitles.ArticleTitleHtml;
            content["articleContentHtml"] = contentTitles.ArticleContentHtml;

            if (result.YamlHeader != null && result.YamlHeader.Count > 0)
            {
                foreach (var item in result.YamlHeader)
                {
                    if (item.Key == "uid")
                    {
                        var uid = item.Value as string;
                        if (!string.IsNullOrWhiteSpace(uid))
                        {
                            model.Uids = new[] { uid }.ToImmutableArray();
                            content["uid"] = item.Value;
                        }
                    }
                    else
                    {
                        content[item.Key] = item.Value;
                        if (item.Key == DocumentTypeKey)
                        {
                            model.DocumentType = item.Value as string;
                        }
                    }
                }
            }
            model.Properties.LinkToFiles = result.LinkToFiles;
            model.Properties.LinkToUids = result.LinkToUids;
            model.Properties.XrefSpec = null;
            if (model.Uids.Length > 0)
            {
                model.Properties.XrefSpec = new XRefSpec
                {
                    Uid = model.Uids[0],
                    Name = TitleThumbnail(content["title"].ToString() ?? model.Uids[0], 30),
                    Href = ((RelativePath)model.File).GetPathFromWorkingFolder()
                };
            }
            model.File = Path.ChangeExtension(model.File, ".json");
        }

        private static string TitleThumbnail(string title, int maxLength)
        {
            if (string.IsNullOrEmpty(title)) return null;
            if (title.Length <= maxLength) return title;
            return title.Substring(0, maxLength) + "...";
        }

        private static Content ExtractContentTitlesFromHtml(string contentHtml)
        {
            var content = new Content();

            var document = new HtmlDocument();
            document.LoadHtml(contentHtml);

            // TODO: how to get TITLE
            // InnerText in HtmlAgilityPack is not decoded, should be a bug
            var headerNode = document.DocumentNode.SelectSingleNode("//h1|//h2|//h3");
            content.Title = StringHelper.HtmlDecode(headerNode?.InnerText);

            if (headerNode != null && document.DocumentNode.FirstChild == headerNode)
            {
                content.ArticleTitleHtml = headerNode.OuterHtml;
                headerNode.Remove();
            }
            else
            {
                content.ArticleTitleHtml = "<h1></h1>";
            }

            content.ArticleContentHtml = document.DocumentNode.OuterHtml;

            return content;
        }

        private class Content
        {
            public string Title { get; set; }

            public string ArticleTitleHtml { get; set; }

            public string ArticleContentHtml { get; set; }
        }
    }
}
