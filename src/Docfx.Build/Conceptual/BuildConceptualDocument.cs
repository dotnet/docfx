// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Net;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.ConceptualDocuments;

[Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
class BuildConceptualDocument : BaseDocumentBuildStep
{
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
        var markdown = (string)content[Constants.PropertyName.Conceptual];
        var result = host.Markup(markdown, model.OriginalFileAndType, false);

        var (h1, h1Raw, conceptual) = ExtractH1(result.Html);
        content["rawTitle"] = h1Raw;
        if (!string.IsNullOrEmpty(h1Raw))
        {
            model.ManifestProperties.rawTitle = h1Raw;
        }
        content[Constants.PropertyName.Conceptual] = conceptual;

        if (result.YamlHeader?.Count > 0)
        {
            foreach (var item in result.YamlHeader.OrderBy(i => i.Key, StringComparer.Ordinal))
            {
                HandleYamlHeaderPair(item.Key, item.Value);
            }
        }

        content[Constants.PropertyName.Title] = GetTitle(result.YamlHeader, h1);
        content["wordCount"] = WordCounter.CountWord(conceptual);

        model.LinkToFiles = result.LinkToFiles.ToImmutableHashSet();
        model.LinkToUids = result.LinkToUids;
        model.FileLinkSources = result.FileLinkSources;
        model.UidLinkSources = result.UidLinkSources;
        model.Properties.XrefSpec = null;

        if (model.Uids.Length > 0)
        {
            var title = content[Constants.PropertyName.Title] as string;
            model.Properties.XrefSpec = new XRefSpec
            {
                Uid = model.Uids[0].Name,
                Name = string.IsNullOrEmpty(title) ? model.Uids[0].Name : title,
                Href = ((RelativePath)model.File).GetPathFromWorkingFolder()
            };
        }

        void HandleYamlHeaderPair(string key, object value)
        {
            switch (key)
            {
                case Constants.PropertyName.Uid:
                    var uid = value as string;
                    if (!string.IsNullOrWhiteSpace(uid))
                    {
                        model.Uids = new[] { new UidDefinition(uid, model.LocalPathFromRoot) }.ToImmutableArray();
                        content[Constants.PropertyName.Uid] = value;
                    }
                    break;
                case DocumentTypeKey:
                    content[key] = value;
                    model.DocumentType = value as string;
                    break;
                case Constants.PropertyName.OutputFileName:
                    content[key] = value;
                    var outputFileName = value as string;
                    if (!string.IsNullOrWhiteSpace(outputFileName))
                    {
                        string fn = null;
                        try
                        {
                            fn = Path.GetFileName(outputFileName);
                        }
                        catch (ArgumentException) { }
                        if (fn == outputFileName)
                        {
                            model.File = (RelativePath)model.File + (RelativePath)outputFileName;
                        }
                        else
                        {
                            Logger.LogWarning($"Invalid output file name in yaml header: {outputFileName}, skip rename output file.");
                        }
                    }
                    break;
                default:
                    content[key] = value;
                    break;
            }
        }

        string GetTitle(ImmutableDictionary<string, object> yamlHeader, string h1)
        {
            // title from YAML header
            if (yamlHeader != null
                && TryGetStringValue(yamlHeader, Constants.PropertyName.Title, out var yamlHeaderTitle))
            {
                return yamlHeaderTitle;
            }

            // title from metadata/titleOverwriteH1
            if (TryGetStringValue(content, Constants.PropertyName.TitleOverwriteH1, out var titleOverwriteH1))
            {
                return titleOverwriteH1;
            }

            // title from H1
            if (!string.IsNullOrEmpty(h1))
            {
                return h1;
            }

            // title from globalMetadata or fileMetadata
            if (TryGetStringValue(content, Constants.PropertyName.Title, out var title))
            {
                return title;
            }

            return default;
        }

        bool TryGetStringValue(IDictionary<string, object> dictionary, string key, out string strValue)
        {
            if (dictionary.TryGetValue(key, out var value) && value is string str && !string.IsNullOrEmpty(str))
            {
                strValue = str;
                return true;
            }
            else
            {
                strValue = null;
                return false;
            }
        }
    }

    static (string h1, string h1Raw, string body) ExtractH1(string contentHtml)
    {
        ArgumentNullException.ThrowIfNull(contentHtml);

        var document = new HtmlDocument();
        document.LoadHtml(contentHtml);

        // InnerText in HtmlAgilityPack is not decoded, should be a bug
        var h1Node = document.DocumentNode.SelectSingleNode("//h1");
        var h1 = WebUtility.HtmlDecode(h1Node?.InnerText);
        var h1Raw = "";
        if (h1Node != null && GetFirstNoneCommentChild(document.DocumentNode) == h1Node)
        {
            h1Raw = h1Node.OuterHtml;
            h1Node.Remove();
        }

        return (h1, h1Raw, document.DocumentNode.OuterHtml);

        static HtmlNode GetFirstNoneCommentChild(HtmlNode node)
        {
            var result = node.FirstChild;
            while (result != null && (result.NodeType == HtmlNodeType.Comment || string.IsNullOrWhiteSpace(result.OuterHtml)))
            {
                result = result.NextSibling;
            }
            return result;
        }
    }
}
