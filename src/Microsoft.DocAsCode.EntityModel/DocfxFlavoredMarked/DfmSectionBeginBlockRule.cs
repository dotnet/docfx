// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml;

    using MarkdownLite;

    public class DfmSectionBeginBlockRule : IMarkdownRule
    {
        public string Name => "Section";

        public static readonly Regex SectionBegin = new Regex(@"^<!--(\s*)((?i)BEGINSECTION)(\s*)(?<attributes>.*?)(\s*)-->(\s*)(?:\n+|$)", RegexOptions.Compiled);

        private static readonly IReadOnlyList<string> ValidAttributes = new List<string>() { "class", "id", "data-resources" };

        private const string SectionReplacementHtmlTag = "div";

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = SectionBegin.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var attributes = ExtractAttibutes(match.Groups["attributes"].Value);
            return new DfmSectionBeginBlockToken(this, attributes);
        }

        private string ExtractAttibutes(string commentText)
        {
            var xmlDoc = new XmlDocument();
            var element = xmlDoc.CreateElement(SectionReplacementHtmlTag);
            try
            {
                element.InnerXml = $"<{SectionReplacementHtmlTag} {commentText} />";
            }
            catch
            {
                Logger.LogWarning($"Parse section syntax error. {commentText} is not a valid attribute");
            }

            var attributes = element.SelectSingleNode($"./{SectionReplacementHtmlTag}[1]").Attributes;
            var attributesToReturn = new StringBuilder();

            foreach (XmlAttribute attr in attributes)
            {
                if (ValidAttributes.Contains(attr.Name))
                {
                    attributesToReturn.Append($@" {attr.Name}=""{HttpUtility.HtmlEncode(attr.Value)}""");
                }
            }
            return attributesToReturn.ToString();
        }
    }
}
