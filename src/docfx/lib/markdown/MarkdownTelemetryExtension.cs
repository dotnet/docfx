// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class MarkdownTelemetryExtension
{
    public static MarkdownPipelineBuilder UseTelemetry(this MarkdownPipelineBuilder builder, DocumentProvider documentProvider)
    {
        return builder.Use(document =>
        {
            var file = ((SourceInfo)InclusionContext.File).File;
            var rootFile = ((SourceInfo)InclusionContext.RootFile).File;
            var elementCount = new Dictionary<string, int>();

            document.Visit(node =>
            {
                var elementType = GetElementType(node);
                elementCount[elementType] = elementCount.TryGetValue(elementType, out var value) ? value + 1 : 1;
                return false;
            });

            Telemetry.TrackMarkdownElement(file, documentProvider.GetContentType(rootFile), documentProvider.GetMime(rootFile), elementCount);
        });
    }

    public static string GetElementType(MarkdownObject node)
    {
        return node switch
        {
            ThematicBreakBlock => "ThematicBreak",
            HeadingBlock headingBlock => headingBlock.HeaderChar == '#' ? "ATXHeading" : "SetextHeading",
            FencedCodeBlock => "FencedCode",
            YamlFrontMatterBlock => "YamlHeader",
            CodeBlock => "IndentedCode",
            HtmlBlock => "HTML",
            LinkReferenceDefinition => "LinkReferenceDefinition",
            ParagraphBlock => "Paragraph",
            BlankLineBlock => "BlankLine",
            QuoteSectionNoteBlock quoteSectionNoteBlock =>
                quoteSectionNoteBlock.QuoteType switch
                {
                    QuoteSectionNoteType.DFMNote => "Note",
                    QuoteSectionNoteType.DFMSection => "SectionDefinition",
                    QuoteSectionNoteType.DFMVideo => "Video",
                    QuoteSectionNoteType.MarkdownQuote => "BlockQuote",
                    _ => quoteSectionNoteBlock.QuoteType.ToString(),
                },
            ListBlock => "List",
            CodeSnippet => "CodeSnippet",
            Table => "Table",
            TabGroupBlock => "TabbedContent",
            MonikerRangeBlock => "MonikerRange",
            RowBlock => "Row",
            NestedColumnBlock => "NestedColumn",
            TripleColonBlock tripleColonBlock => $"TripleColon{StringUtility.UpperCaseFirstChar(tripleColonBlock.Extension.Name)}",
            InclusionBlock => "IncludeFile",
            InclusionInline => "IncludeFile",
            EmojiInline => "Emoji",
            LiteralInline literalInline => literalInline.IsFirstCharacterEscaped ? "BackslashEscape" : "TextualContent",
            HtmlEntityInline => "HTMLEntity",
            CodeInline => "CodeSpan",
            EmphasisInline emphasisInline => emphasisInline.DelimiterCount == 2 ? "StrongEmphasis" : "Emphasis",
            LinkInline linkInline when linkInline.IsImage => "Image",
            LinkInline linkInline when linkInline.IsAutoLink => "Autolink",
            LinkInline => "Link",
            AutolinkInline => "Autolink",
            HtmlInline => "RawHTML",
            LineBreakInline linkBreakInline => linkBreakInline.IsHard ? "HardLineBreak" : "SoftLineBreak",
            XrefInline => "Xref",
            NolocInline => "Noloc",
            _ => node.GetType().Name,
        };
    }
}
