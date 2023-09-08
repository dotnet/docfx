// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class QuoteSectionNoteRender : HtmlObjectRenderer<QuoteSectionNoteBlock>
{
    private readonly MarkdownContext _context;
    private readonly Dictionary<string, string> _notes;

    public QuoteSectionNoteRender(MarkdownContext context, Dictionary<string, string> notes)
    {
        _context = context;
        _notes = notes;
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
        var noteHeadingText = _context.GetToken(obj.NoteTypeString.ToLower()) ?? obj.NoteTypeString.ToUpper();

        // Trim <h5></h5> for backward compatibility
        if (noteHeadingText.StartsWith("<h5>") && noteHeadingText.EndsWith("</h5>"))
        {
            noteHeadingText = noteHeadingText[4..^5];
        }

        var noteHeading = $"<h5>{HttpUtility.HtmlEncode(noteHeadingText)}</h5>";
        var classNames = _notes.TryGetValue(obj.NoteTypeString, out var value) ? value : obj.NoteTypeString.ToUpper();
        renderer.Write("<div").Write($" class=\"{classNames}\"").WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteLine(noteHeading);
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</div>");
    }

    private void WriteSection(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        string attribute = string.IsNullOrEmpty(obj.SectionAttributeString) ?
                    string.Empty :
                    $" {obj.SectionAttributeString}";
        renderer.Write("<div").Write(attribute).WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</div>");
    }

    private void WriteQuote(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        renderer.Write("<blockquote").WriteAttributes(obj).WriteLine(">");
        var savedImplicitParagraph = renderer.ImplicitParagraph;
        renderer.ImplicitParagraph = false;
        renderer.WriteChildren(obj);
        renderer.ImplicitParagraph = savedImplicitParagraph;
        renderer.WriteLine("</blockquote>");
    }

    private void WriteVideo(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
    {
        var modifiedLink = string.Empty;
        var modifiedTitle = string.Empty;

        if (!string.IsNullOrWhiteSpace(obj?.VideoLink))
        {
            modifiedLink = FixUpLink(obj.VideoLink);
        }
        if (!string.IsNullOrWhiteSpace(obj?.VideoTitle))
        {
            modifiedTitle = FixUpTitle(obj.VideoTitle);
        }

        renderer.Write("<div class=\"embeddedvideo\"").WriteAttributes(obj).Write(">");
        renderer.Write($"<iframe src=\"{modifiedLink}\" title=\"{modifiedTitle}\"frameborder=\"0\" allowfullscreen=\"true\"></iframe>");
        renderer.WriteLine("</div>");
    }

    public static string FixUpTitle(string title)
    {
        if (title.StartsWith("'") || title.StartsWith('"'))
        {
            title = title.Substring(1);
        }
        if (title.EndsWith("'") || title.EndsWith('"'))
        {
            title = title.Substring(0, title.Length - 1);
        }
        return title;
    }

    public static string FixUpLink(string link)
    {
        if (!link.Contains("https"))
        {
            link = link.Replace("http", "https");
        }
        if (Uri.TryCreate(link, UriKind.Absolute, out Uri videoLink))
        {
            var host = videoLink.Host;
            var query = videoLink.Query;
            if (query.Length > 1)
            {
                query = query.Substring(1);
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
                    query = query + "&nocookie=true";
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
}
