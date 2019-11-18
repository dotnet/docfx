// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net;

    using Markdig.Parsers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;

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

        public string GitUrl { get; set; }

        public bool IsInteractive { get; set; }

        public bool IsNotebookCode { get; set; }

        public void SetAttributeString()
        {
            var attributes = this.GetAttributes();

            if (!string.IsNullOrEmpty(Language))
            {
                attributes.AddClass($"lang-{Language}");
            }

            if (IsInteractive)
            {
                attributes.AddProperty("data-interactive", WebUtility.HtmlEncode(Language));
            }

            if (GitUrl != null && GitUrl.StartsWith("https://github.com", StringComparison.OrdinalIgnoreCase))
            {
                attributes.AddProperty("data-src", WebUtility.HtmlEncode(GitUrl));
            }

            if (!string.IsNullOrEmpty(Name))
            {
                attributes.AddProperty("name", Name);
            }

            if (!string.IsNullOrEmpty(Title))
            {
                attributes.AddProperty("title", Title);
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
