// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;
using Xunit;

namespace Microsoft.Docs.Build;

public static class MarkdownElementTelemetryExtensionTest
{
    [Theory]
    [MemberData(nameof(ElementTypeTestData))]
    public static void GetElementTypeTest(MarkdownObject node, string expectedElementType)
    {
        Assert.Equal(expectedElementType, MarkdownTelemetryExtension.GetElementType(node));
    }

    public static IEnumerable<object[]> ElementTypeTestData =>
        new List<object[]>
        {
                new object[] { new ThematicBreakBlock(null), "ThematicBreak" },
                new object[] { new HeadingBlock(null) { HeaderChar = '#' }, "ATXHeading" },
                new object[] { new HeadingBlock(null) { HeaderChar = '\0' }, "SetextHeading" },
                new object[] { new CodeBlock(null), "IndentedCode" },
                new object[] { new FencedCodeBlock(null), "FencedCode" },
                new object[] { new HtmlBlock(null), "HTML" },
                new object[] { new LinkReferenceDefinition(), "LinkReferenceDefinition" },
                new object[] { new ParagraphBlock(), "Paragraph" },
                new object[] { new BlankLineBlock(), "BlankLine" },
                new object[] { new QuoteSectionNoteBlock(null) { QuoteType = QuoteSectionNoteType.MarkdownQuote }, "BlockQuote" },
                new object[] { new QuoteSectionNoteBlock(null) { QuoteType = QuoteSectionNoteType.DFMSection }, "SectionDefinition" },
                new object[] { new QuoteSectionNoteBlock(null) { QuoteType = QuoteSectionNoteType.DFMNote }, "Note" },
                new object[] { new QuoteSectionNoteBlock(null) { QuoteType = QuoteSectionNoteType.DFMVideo }, "Video" },
                new object[] { new ListBlock(null), "List" },
                new object[] { new CodeSnippet(null), "CodeSnippet" },
                new object[] { new Table(), "Table" },
                new object[] { new TabGroupBlock(new List<TabItemBlock>() { new TabItemBlock("Fake", "Fake", new TabTitleBlock(), new TabContentBlock(new List<Block>()), false) }.ToImmutableArray(), 0, 0, 0), "TabbedContent" },
                new object[] { new MonikerRangeBlock(null), "MonikerRange" },
                new object[] { new RowBlock(null), "Row" },
                new object[] { new NestedColumnBlock(null), "NestedColumn" },
                new object[] { new TripleColonBlock(null) { Extension = new ZoneExtension() }, "TripleColonZone" },
                new object[] { new TripleColonBlock(null) { Extension = new ChromelessFormExtension() }, "TripleColonForm" },
                new object[] { new TripleColonBlock(null) { Extension = new ImageExtension(null) }, "TripleColonImage" },
                new object[] { new TripleColonBlock(null) { Extension = new CodeExtension(null) }, "TripleColonCode" },
                new object[] { new YamlFrontMatterBlock(null), "YamlHeader" },
                new object[] { new InclusionBlock(null), "IncludeFile" },
                new object[] { new InclusionInline(), "IncludeFile" },
                new object[] { new LiteralInline() { IsFirstCharacterEscaped = true }, "BackslashEscape" },
                new object[] { new LiteralInline() { IsFirstCharacterEscaped = false }, "TextualContent" },
                new object[] { new HtmlEntityInline(), "HTMLEntity" },
                new object[] { new CodeInline(), "CodeSpan" },
                new object[] { new EmphasisInline() { DelimiterCount = 1 }, "Emphasis" },
                new object[] { new EmphasisInline() { DelimiterCount = 2 }, "StrongEmphasis" },
                new object[] { new LinkInline() { IsImage = false, IsAutoLink = false }, "Link" },
                new object[] { new LinkInline() { IsImage = true, IsAutoLink = false }, "Image" },
                new object[] { new LinkInline() { IsImage = false, IsAutoLink = true }, "Autolink" },
                new object[] { new AutolinkInline(), "Autolink" },
                new object[] { new HtmlInline(), "RawHTML" },
                new object[] { new LineBreakInline() { IsHard = true }, "HardLineBreak" },
                new object[] { new LineBreakInline() { IsHard = false }, "SoftLineBreak" },
                new object[] { new XrefInline(), "Xref" },
                new object[] { new EmojiInline(), "Emoji" },
                new object[] { new NolocInline(), "Noloc" },
        };
}
