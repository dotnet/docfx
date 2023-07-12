// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class QuoteSectionNoteBlock : ContainerBlock
{
    public QuoteSectionNoteBlock(BlockParser parser) : base(parser)
    {
    }

    public char QuoteChar { get; set; }

    public QuoteSectionNoteType QuoteType { get; set; }

    public string SectionAttributeString { get; set; }

    public string NoteTypeString { get; set; }

    public string VideoLink { get; set; }
}

public enum QuoteSectionNoteType
{
    MarkdownQuote = 0,
    DFMSection,
    DFMNote,
    DFMVideo
}
