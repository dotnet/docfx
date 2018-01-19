// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;

    public class QuoteSectionNoteRender : HtmlObjectRenderer<QuoteSectionNoteBlock>
    {
        private ImmutableDictionary<string, string> _tokens;

        public QuoteSectionNoteRender(ImmutableDictionary<string, string> tokens)
        {
            _tokens = tokens;
        }

        protected override void Write(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
        {
            renderer.EnsureLine();
            bool savedImplicitParagraph;
            switch (obj.QuoteType)
            {
                case QuoteSectionNoteType.MarkdownQuote:
                    renderer.Write("<blockquote").WriteAttributes(obj).WriteLine(">");
                    savedImplicitParagraph = renderer.ImplicitParagraph;
                    renderer.ImplicitParagraph = false;
                    renderer.WriteChildren(obj);
                    renderer.ImplicitParagraph = savedImplicitParagraph;
                    renderer.WriteLine("</blockquote>");
                    break;
                case QuoteSectionNoteType.DFMSection:
                    string attribute = string.IsNullOrEmpty(obj.SectionAttributeString) ?
                                       string.Empty :
                                       $" {obj.SectionAttributeString}";
                    renderer.Write("<div").Write(attribute).WriteAttributes(obj).WriteLine(">");
                    savedImplicitParagraph = renderer.ImplicitParagraph;
                    renderer.ImplicitParagraph = false;
                    renderer.WriteChildren(obj);
                    renderer.ImplicitParagraph = savedImplicitParagraph;
                    renderer.WriteLine("</div>");
                    break;
                case QuoteSectionNoteType.DFMNote:
                    string noteHeading = string.Empty;
                    if (_tokens?.TryGetValue(obj.NoteTypeString.ToLower(), out noteHeading) != true)
                    {
                        noteHeading = $"<h5>{obj.NoteTypeString.ToUpper()}</h5>";
                    };
                    renderer.Write("<div").Write($" class=\"{obj.NoteTypeString.ToUpper()}\"").WriteAttributes(obj).WriteLine(">");
                    savedImplicitParagraph = renderer.ImplicitParagraph;
                    renderer.ImplicitParagraph = false;
                    renderer.WriteLine(noteHeading);
                    renderer.WriteChildren(obj);
                    renderer.ImplicitParagraph = savedImplicitParagraph;
                    renderer.WriteLine("</div>");
                    break;
                case QuoteSectionNoteType.DFMVideo:
                    renderer.Write("<div class=\"embeddedvideo\"").WriteAttributes(obj).Write(">");
                    renderer.Write($"<iframe src=\"{obj.VideoLink}\" frameborder=\"0\" allowfullscreen=\"true\"></iframe>");
                    renderer.WriteLine("</div>");
                    break;
                default:
                    break;
            }
        }
    }
}
