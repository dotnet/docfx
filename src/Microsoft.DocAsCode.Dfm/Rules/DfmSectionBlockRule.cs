// Copyright (c) Microsoft. All rights reserved.
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
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class DfmSectionBlockRule : IMarkdownRule
    {
        private static readonly Matcher _SectionMatcher =
            Matcher.WhiteSpacesOrEmpty + Matcher.CaseInsensitiveString("[!div") +
            (
                Matcher.WhiteSpaces +
                (
                    (Matcher.Char('`') + Matcher.AnyCharNotIn('`', '\n').RepeatAtLeast(0).ToGroup("attributes") + '`') |
                    Matcher.AnyCharNotIn(']', '\n').RepeatAtLeast(0).ToGroup("attributes")
                )
            ).Maybe() +
            ']' + Matcher.WhiteSpacesOrEmpty + Matcher.NewLine.RepeatAtLeast(0);
        private static readonly Regex _sectionRegex = new Regex(@"^(?<rawmarkdown> *\[\!div( +(?<quote>`?)(?<attributes>.*?)(\k<quote>))?\]\s*\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        public string Name => "DfmSection";

        private const string SectionReplacementHtmlTag = "div";

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            if (parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(_SectionMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                var attributes = ExtractAttibutes(match.GetGroup("attributes")?.GetValue() ?? string.Empty);
                return new DfmSectionBlockToken(this, parser.Context, attributes, sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
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
