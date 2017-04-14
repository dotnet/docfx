// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ConceptualDocuments
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildConceptualDocument : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        private const string ConceptualKey = Constants.PropertyName.Conceptual;
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
            var markdown = (string)content[ConceptualKey];
            var result = host.Markup(markdown, model.OriginalFileAndType);

            var htmlInfo = SeparateHtml(result.Html);
            model.Properties.IsUserDefinedTitle = false;
            content[Constants.PropertyName.Title] = htmlInfo.Title;
            content["rawTitle"] = htmlInfo.RawTitle;
            content[ConceptualKey] = htmlInfo.Content;

            if (result.YamlHeader?.Count > 0)
            {
                foreach (var item in result.YamlHeader)
                {
                    if (item.Key == Constants.PropertyName.Uid)
                    {
                        var uid = item.Value as string;
                        if (!string.IsNullOrWhiteSpace(uid))
                        {
                            model.Uids = new[] { new UidDefinition(uid, model.LocalPathFromRoot) }.ToImmutableArray();
                            content[Constants.PropertyName.Uid] = item.Value;
                        }
                    }
                    else
                    {
                        content[item.Key] = item.Value;
                        if (item.Key == DocumentTypeKey)
                        {
                            model.DocumentType = item.Value as string;
                        }
                        else if (item.Key == Constants.PropertyName.Title)
                        {
                            model.Properties.IsUserDefinedTitle = true;
                        }
                        else if (item.Key == Constants.PropertyName.OutputFileName)
                        {
                            var outputFileName = item.Value as string;
                            if (!string.IsNullOrWhiteSpace(outputFileName))
                            {
                                model.File = (RelativePath)model.File + (RelativePath)outputFileName;
                            }
                        }
                    }
                }
            }
            model.LinkToFiles = result.LinkToFiles.ToImmutableHashSet();
            model.LinkToUids = result.LinkToUids;
            model.FileLinkSources = result.FileLinkSources;
            model.UidLinkSources = result.UidLinkSources;
            model.Properties.XrefSpec = null;
            if (model.Uids.Length > 0)
            {
                model.Properties.XrefSpec = new XRefSpec
                {
                    Uid = model.Uids[0].Name,
                    Name = content[Constants.PropertyName.Title] as string ?? model.Uids[0].Name,
                    Href = ((RelativePath)model.File).GetPathFromWorkingFolder()
                };
            }

            foreach (var d in result.Dependency)
            {
                host.ReportDependencyTo(model, d, DependencyTypeName.Include);
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion

        private static HtmlInfo SeparateHtml(string contentHtml)
        {
            var content = new HtmlInfo();

            var document = new HtmlDocument();
            document.LoadHtml(contentHtml);

            // TODO: how to get TITLE
            // InnerText in HtmlAgilityPack is not decoded, should be a bug
            var headerNode = document.DocumentNode.SelectSingleNode("//h1|//h2|//h3");
            content.Title = StringHelper.HtmlDecode(headerNode?.InnerText);

            if (headerNode != null && GetFirstNoneCommentChild(document.DocumentNode) == headerNode)
            {
                content.RawTitle = headerNode.OuterHtml;
                headerNode.Remove();
            }
            else
            {
                content.RawTitle = string.Empty;
            }

            content.Content = document.DocumentNode.OuterHtml;

            return content;
        }

        private static HtmlNode GetFirstNoneCommentChild(HtmlNode node)
        {
            var result = node.FirstChild;
            while (result != null && (result.NodeType == HtmlNodeType.Comment || string.IsNullOrWhiteSpace(result.OuterHtml)))
            {
                result = result.NextSibling;
            }
            return result;
        }

        private class HtmlInfo
        {
            public string Title { get; set; }

            public string RawTitle { get; set; }

            public string Content { get; set; }
        }
    }
}
