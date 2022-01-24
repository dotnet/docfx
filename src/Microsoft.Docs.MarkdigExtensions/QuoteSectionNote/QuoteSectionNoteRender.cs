// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class QuoteSectionNoteRender : HtmlObjectRenderer<QuoteSectionNoteBlock>
{
    private readonly MarkdownContext _context;

    public QuoteSectionNoteRender(MarkdownContext context)
    {
        _context = context;
    }

    public static string FixUpLink(string link)
    {
        if (!link.Contains("https"))
        {
            link = link.Replace("http", "https");
        }
        if (Uri.TryCreate(link, UriKind.Absolute, out var videoLink))
        {
            var host = videoLink.Host;
            var query = videoLink.Query;
            if (query.Length > 1)
            {
                query = query[1..];
            }

            if (host.Equals("channel9.msdn.com", StringComparison.OrdinalIgnoreCase))
            {
                // case 1, Channel 9 video, need to add query string param
                if (string.IsNullOrWhiteSpace(query))
                {
                    query = "nocookie=true";
                }
                else
                {
                    query += "&nocookie=true";
                }
            }
            else if (host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) || host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                // case 2, YouTube video
                host = "www.youtube-nocookie.com";
            }

            var builder = new UriBuilder(videoLink) { Host = host, Query = query };
            link = builder.Uri.ToString();
        }

        return link;
    }

    protected override void Write(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        renderer.EnsureLine();
        switch (obj.QuoteType)
        {
            case QuoteSectionNoteType.MarkdownQuote:
                WriteQuote(renderer, obj);
                break;
            case QuoteSectionNoteType.DFMSection:
                WriteSection(renderer, obj);
                break;
            case QuoteSectionNoteType.DFMNote:
                WriteNote(renderer, obj);
                break;
            case QuoteSectionNoteType.DFMVideo:
                WriteVideo(renderer, obj);
                break;
            default:
                break;
        }
    }

    private void WriteNote(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        var noteHeading = _context.GetToken(obj.NoteTypeString.ToLowerInvariant()) ?? $"<h5>{obj.NoteTypeString.ToUpperInvariant()}</h5>";
        renderer.Write("<div").Write($" class=\"{obj.NoteTypeString.ToUpperInvariant()}\"").WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteLine(noteHeading);
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</div>");
    }

    private static void WriteSection(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        var attribute = string.IsNullOrEmpty(obj.SectionAttributeString) ?
                    "" :
                    $" {obj.SectionAttributeString}";
        renderer.Write("<div").Write(attribute).WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</div>");
    }

    private static void WriteQuote(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        renderer.Write("<blockquote").WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</blockquote>");
    }

    private static void WriteVideo(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        var modifiedLink = "";

        if (!string.IsNullOrWhiteSpace(obj?.VideoLink))
        {
            modifiedLink = FixUpLink(obj.VideoLink);
        }

        renderer.Write("<div class=\"embeddedvideo\"").WriteAttributes(obj).Write(">");
        renderer.Write($"<iframe src=\"{modifiedLink}\" frameborder=\"0\" allowfullscreen=\"true\"></iframe>");
        renderer.WriteLine("</div>");
    }
}
