// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMigrationHtmlMetadataBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.Migration.Properties";

        private static readonly Regex _azureHtmlMetadataRegex = new Regex(@"^(?: *(\<(properties|tags)\s+[^\>]*\s*\>[^\<]*\<\/\1\>|\<(?:properties|tags)\s+[^>]*\/>)\s*){1,2}(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _azureHtmlTitleRegex = new Regex("(\\|+.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureHtmlMetadataRegex => _azureHtmlMetadataRegex;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            var match = AzureHtmlMetadataRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }

            var sourceInfo = context.Consume(match.Length);
            if (!engine.Context.Variables.TryGetValue("path", out object currentFilePath))
            {
                Logger.LogWarning($"Can't get path for setting azure ms.assetid. Won't set it.");
                currentFilePath = string.Empty;
            }

            var metadata = GetAttributesFromHtmlContent(match.Value);
            if (metadata == null)
            {
                return new MarkdownTextToken(this, engine.Context, match.Value, sourceInfo);
            }
            return new AzureHtmlMetadataBlockToken(this, engine.Context, metadata.Properties, metadata.Tags, sourceInfo);
        }

        private class AzureHtmlMetadata
        {
            public Dictionary<string, string> Properties { get; set; }

            public Dictionary<string, string> Tags { get; set; }
        }

        private AzureHtmlMetadata GetAttributesFromHtmlContent(string htmlContent)
        {
            AzureHtmlMetadata azureHtmlMetadata = new AzureHtmlMetadata();
            try
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);
                var propertiesNode = htmlDoc.DocumentNode.SelectSingleNode("//properties");
                azureHtmlMetadata.Properties = GetAttributesFromNode(propertiesNode);
                var tagsNode = htmlDoc.DocumentNode.SelectSingleNode("//tags");
                azureHtmlMetadata.Tags = GetAttributesFromNode(tagsNode);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Parse azure html metadata error. {htmlContent} is not a valid html. ex: {e}");
                return null;
            }

            return azureHtmlMetadata;
        }

        private Dictionary<string, string> GetAttributesFromNode(HtmlNode node)
        {
            var attributes = new Dictionary<string, string>();
            if (node != null)
            {
                foreach (var attribute in node.Attributes)
                {
                    if (string.Equals(attribute.Name, "pageTitle", StringComparison.OrdinalIgnoreCase))
                    {
                        // Per Azure's request, migration script should remove everything after "|" and put "Microsoft Docs" there for title.
                        // After migration, the title should always look like "xxxxxxxx | Microsoft Docs" and only one "|" is in title.
                        var title = attribute.Value;
                        if (title != null)
                        {
                            title = _azureHtmlTitleRegex.Replace(title, match => "| Microsoft Docs");
                        }
                        attributes["title"] = title;
                    }
                    else if (string.Equals(attribute.Name, "authors", StringComparison.OrdinalIgnoreCase))
                    {
                        var authors = attribute.Value.Split( new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (authors.Length >= 1)
                        {
                            attributes["author"] = authors[0].Trim();
                        }
                    }
                    else
                    {
                        attributes[attribute.Name] = attribute.Value;
                    }
                }
            }
            return attributes;
        }
    }
}
