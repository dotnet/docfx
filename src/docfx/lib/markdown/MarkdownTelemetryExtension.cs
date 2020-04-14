// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTelemetryExtension
    {
        public static MarkdownPipelineBuilder UseTelemetry(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    Telemetry.TrackMarkdownElement((Document)InclusionContext.File, GetElementType(node), GetTokenType(node));
                    return false;
                });
            });
        }

        public static string? GetElementType(MarkdownObject node)
        {
            return node switch
            {
                ThematicBreakBlock thematicBreak => "ThematicBreak",
                HeadingBlock headingBlock => headingBlock.HeaderChar == '#' ? "ATXHeading" : "SetextHeading",
                FencedCodeBlock fencedCodeBlock => "FencedCode",
                YamlFrontMatterBlock yamlFrontMatterBlock => "YamlHeader",
                CodeBlock codeBlock => "IndentedCode",
                HtmlBlock htmlBlock => "HTML",
                LinkReferenceDefinition linkReferenceDefinition => "LinkReferenceDefinition",
                ParagraphBlock paragraphBlock => "Paragraph",
                BlankLineBlock blankLineBlock => "BlankLine",
                QuoteSectionNoteBlock quoteSectionNoteBlock =>
                    quoteSectionNoteBlock.QuoteType switch
                    {
                        QuoteSectionNoteType.DFMNote => "Note",
                        QuoteSectionNoteType.DFMSection => "SectionDefinition",
                        QuoteSectionNoteType.DFMVideo => "Video",
                        QuoteSectionNoteType.MarkdownQuote => "BlockQuote",
                        _ => throw new NotSupportedException(),
                    },
                ListBlock listBlock => "List",
                CodeSnippet codeSnippet => "CodeSnippet",
                Table table => "Table",
                TabGroupBlock tabGroupBlock => "TabbedContent",
                MonikerRangeBlock monikerRangeBlock => "MonikerRange",
                RowBlock rowBlock => "Row",
                NestedColumnBlock nestedColumnBlock => "NestedColumn",
                TripleColonBlock tripleColonBlock => $"TripleColon{StringUtility.UpperCaseFirstChar(tripleColonBlock.Extension.Name)}",
                InclusionBlock inclusionBlock => "IncludeFile",
                InclusionInline inclusionInline => "IncludeFile",
                EmojiInline emojiInline => "Emoji",
                LiteralInline literalInline => literalInline.IsFirstCharacterEscaped ? "BlackslashEscape" : "TextualContent",
                HtmlEntityInline htmlEntityInline => "HTMLEntity",
                CodeInline codeInline => "CodeSpan",
                EmphasisInline emphasisInline => emphasisInline.DelimiterCount == 2 ? "StrongEmphasis" : "Emphasis",
                LinkInline linkInline when linkInline.IsImage => "Image",
                LinkInline linkInline when linkInline.IsAutoLink => "Autolink",
                LinkInline linkInline => "Link",
                AutolinkInline autolinkInline => "Autolink",
                HtmlInline htmlInline => "RawHTML",
                LineBreakInline linkBreakInline => linkBreakInline.IsHard ? "HardLineBreak" : "SoftLineBreak",
                XrefInline xrefInline => "Xref",
                NolocInline nolocInline => "Noloc",
                _ => null,
            };
        }

        public static string? GetTokenType(MarkdownObject node)
        {
            var tokenType = node.GetType().Name;
            tokenType += node switch
            {
                HtmlBlock htmlBlock => $"-{htmlBlock.Type}",
                QuoteSectionNoteBlock quoteSectionNoteBlock => GetQuoteSectionNoteBlockSubType(quoteSectionNoteBlock),
                TripleColonBlock tripleColonBlock => $"-{StringUtility.UpperCaseFirstChar(tripleColonBlock.Extension.Name)}",
                _ => null,
            };

            return tokenType;

            string GetQuoteSectionNoteBlockSubType(QuoteSectionNoteBlock quoteSectionNote)
            {
                var subType = $"-{quoteSectionNote.QuoteType}";
                if (quoteSectionNote.QuoteType == QuoteSectionNoteType.DFMNote)
                {
                    subType += $"-{StringUtility.UpperCaseFirstChar(quoteSectionNote.NoteTypeString)}";
                }
                return subType;
            }
        }
    }
}
