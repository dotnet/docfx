// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureHtmlMetadataBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.Properties";

        private static readonly Regex _azureHtmlMetadataRegex = new Regex(@"^(?: *(\<(properties|tags)\s+[^\>]*\s*\>[^\<]*\<\/\1\>|\<(?:properties|tags)\s+[^>]*\/>)\s*){1,2}(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureHtmlMetadataRegex => _azureHtmlMetadataRegex;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = AzureHtmlMetadataRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }

            source = source.Substring(match.Length);
            var metadata = GetAttributesFromHtmlContent(match.Value);
            if (metadata == null)
            {
                return new MarkdownTextToken(this, engine.Context, match.Value, match.Value);
            }
            return new AzureHtmlMetadataBlockToken(this, engine.Context, metadata.Properties, metadata.Tags, match.Value);
        }

        private class AzureHtmlMetadata
        {
            public IReadOnlyDictionary<string, string> Properties { get; set; }

            public IReadOnlyDictionary<string, string> Tags { get; set; }
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
            catch(Exception e)
            {
                Logger.LogWarning($"Parse azure html metadata error. {htmlContent} is not a valid html. ex: {e}");
                return null;
            }

            return azureHtmlMetadata;
        }

        private IReadOnlyDictionary<string, string> GetAttributesFromNode(HtmlNode node)
        {
            var attributes = new Dictionary<string, string>();
            foreach(var attribute in node.Attributes)
            {
                // TODO: Azure has metadata with name "authors" that could be a list of name. Not sure if docfx can handle it.
                if (string.Equals(attribute.Name, "pageTitle", StringComparison.OrdinalIgnoreCase))
                {
                    attributes.Add("title", attribute.Value);
                }
                else
                {
                    attributes.Add(attribute.Name, attribute.Value);
                }
            }
            return attributes;
        }
    }
}
