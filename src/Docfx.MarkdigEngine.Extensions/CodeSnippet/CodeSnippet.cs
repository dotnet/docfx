// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

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

        if (!string.IsNullOrEmpty(Language))
        {
            sb.Append($@" class=""lang-{Language}""");
        }

        if (!string.IsNullOrEmpty(Name))
        {
            sb.Append($@" name=""{Name}""");
        }

        if (!string.IsNullOrEmpty(Title))
        {
            sb.Append($@" title=""{Title}""");
        }

        var highlightRangesString = GetHighlightLinesString();

        if (highlightRangesString != string.Empty)
        {
            sb.Append($@" highlight-lines=""{highlightRangesString}""");
        }

        return sb.ToString();
    }

    public string GetHighlightLinesString()
    {
        if (HighlightRanges != null && HighlightRanges.Any())
        {
            return string.Join(',', HighlightRanges.Select(highlight =>
            {
                if (highlight.Start == highlight.End) return highlight.Start.ToString();

                if (highlight.End == int.MaxValue) return highlight.Start + "-";

                return highlight.Start + "-" + highlight.End;
            }));
        }

        return string.Empty;
    }
}
