﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmSectionBlockRule : IMarkdownRule
    {
        public string Name => "DfmSection";

        private static readonly Regex _sectionRegex = new Regex(@"^(?<rawmarkdown> *\[\!div( +(?<quote>`?)(?<attributes>.*?)(\k<quote>))?\]\s*\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        private const string SectionReplacementHtmlTag = "div";

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            var match = _sectionRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Groups["rawmarkdown"].Length);
            var attributes = ExtractAttibutes(match.Groups["attributes"].Value);
            return new DfmSectionBlockToken(this, parser.Context, attributes, sourceInfo);
        }

        private string ExtractAttibutes(string attributeText)
        {
            var xmlDoc = new XmlDocument();
            var element = xmlDoc.CreateElement(SectionReplacementHtmlTag);
            try
            {
                element.InnerXml = $"<{SectionReplacementHtmlTag} {attributeText} />";
            }
            catch
            {
                Logger.LogWarning($"Parse section syntax error. {attributeText} is not a valid attribute");
                return string.Empty;
            }

            var attributes = element.SelectSingleNode($"./{SectionReplacementHtmlTag}[1]").Attributes;
            var attributesToReturn = new StringBuilder();
            foreach (XmlAttribute attr in attributes)
            {
                attributesToReturn.Append($@" {attr.Name}=""{HttpUtility.HtmlEncode(attr.Value)}""");
            }
            return attributesToReturn.ToString();
        }
    }
}
