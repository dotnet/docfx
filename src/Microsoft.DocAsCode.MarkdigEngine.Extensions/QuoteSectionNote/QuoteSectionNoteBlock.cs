// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;

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
}
