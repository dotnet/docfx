// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Markdig.Parsers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    public class CodeSnippet : LeafBlock
    {
        public CodeSnippet(BlockParser parser) : base(parser)
        {
        }

        public string Name { get; set; }

        public string Language { get; set; }

        public string CodePath { get; set; }

        public string TagName { get; set; }

        public CodeRange StartEndRange { get; set; }

        public CodeRange BookMarkRange { get; set; }

        public List<CodeRange> CodeRanges { get; set; }

        public List<CodeRange> HighlightRanges { get; set; }

        public int? DedentLength { get; set; }

        public string Title { get; set; }

        public string Raw { get; set; }

        public void SetAttributeString()
        {
            var attributes = this.GetAttributes();

            if (!string.IsNullOrEmpty(this.Language))
            {
                attributes.AddClass($"lang-{this.Language}");
            }

            if (!string.IsNullOrEmpty(this.Name))
            {
                attributes.AddProperty("name", this.Name);
            }

            if (!string.IsNullOrEmpty(this.Title))
            {
                attributes.AddProperty("title", this.Title);
            }

            var highlightRangesString = GetHighlightLinesString();
            if (highlightRangesString != string.Empty)
            {
                attributes.AddProperty("highlight-lines", highlightRangesString);
            }
        }

        // retired because the value need escaping
        public string ToAttributeString()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(this.Language))
            {
                sb.Append(string.Format(@" class=""lang-{0}""", this.Language));
            }

            if (!string.IsNullOrEmpty(this.Name))
            {
                sb.Append(string.Format(@" name=""{0}""", this.Name));
            }

            if (!string.IsNullOrEmpty(this.Title))
            {
                sb.Append(string.Format(@" title=""{0}""", this.Title));
            }

            var highlightRangesString = GetHighlightLinesString();

            if(highlightRangesString != string.Empty)
            {
                sb.Append(string.Format(@" highlight-lines=""{0}""", highlightRangesString));
            }

            return sb.ToString();
        }

        public string GetHighlightLinesString()
        {
            if (this.HighlightRanges != null && this.HighlightRanges.Any())
            {
                return string.Join(",", this.HighlightRanges.Select(highlight =>
                {
                    if (highlight.Start == highlight.End) return highlight.Start.ToString();

                    if (highlight.End == int.MaxValue) return highlight.Start + "-";

                    return highlight.Start + "-" + highlight.End;
                }));
            }

            return string.Empty;
        }
    }
}
