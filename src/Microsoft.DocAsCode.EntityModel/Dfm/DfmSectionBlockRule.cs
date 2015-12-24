// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml;

    using MarkdownLite;

    public class DfmSectionBlockRule : IMarkdownRule
    {
        public string Name => "Section";

        public static readonly Regex _sectionRegex = new Regex(@"^(?<rawmarkdown> *\[\!div( +(?<quote>`?)(?<attributes>.*?)(\k<quote>))?\]\s*(?:\n|$))", RegexOptions.Compiled);

        private const string SectionReplacementHtmlTag = "div";

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            if (!engine.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)engine.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            var match = _sectionRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var attributes = ExtractAttibutes(match.Groups["attributes"].Value);
            return new DfmSectionBlockToken(this, engine.Context, attributes, match.Groups["rawmarkdown"].Value);
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
