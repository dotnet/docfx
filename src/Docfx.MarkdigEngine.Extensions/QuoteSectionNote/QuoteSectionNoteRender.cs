// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Web;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public partial class QuoteSectionNoteRender : HtmlObjectRenderer<QuoteSectionNoteBlock>
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

    private static void WriteSection(HtmlRenderer renderer, QuoteSectionNoteBlock obj)
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
        var modifiedLink = string.Empty;

        if (!string.IsNullOrWhiteSpace(obj?.VideoLink))
        {
            modifiedLink = FixUpLink(obj.VideoLink);
        }

        renderer.Write("<div class=\"embeddedvideo\"").WriteAttributes(obj).Write(">");
        renderer.Write($"<iframe src=\"{modifiedLink}\" frameborder=\"0\" allowfullscreen=\"true\"></iframe>");
        renderer.WriteLine("</div>");
    }

    public static string FixUpLink(string link)
    {
        if (link.StartsWith("http:"))
        {
            link = "https:" + link.Substring("http:".Length);
        }
        if (Uri.TryCreate(link, UriKind.Absolute, out Uri videoLink))
        {
            var host = videoLink.Host;
            var path = videoLink.LocalPath;
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
                    query += "&nocookie=true";
                }
            }
            else if (hostsYouTube.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                // case 2, YouTube video
                var idYouTube = GetYouTubeId(host, path, ref query);
                if (idYouTube != null)
                {
                    host = "www.youtube-nocookie.com";
                    path = "/embed/" + idYouTube;
                    query = AddYouTubeRel(query);
                }
                else
                {
                    //YouTube Playlist
                    var listYouTube = GetYouTubeList(query);
                    if (listYouTube != null)
                    {
                        host = "www.youtube-nocookie.com";
                        path = "/embed/videoseries";
                        query = "list=" + listYouTube;
                        query = AddYouTubeRel(query);
                    }
                }

                //Keep this to preserve previous behavior
                if (host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) || host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase))
                {
                    host = "www.youtube-nocookie.com";
                }
            }

            var builder = new UriBuilder(videoLink) { Host = host, Path = path, Query = query };
            link = builder.Uri.ToString();
        }

        return link;
    }

    /// <summary>
    /// Only related videos from the same channel
    /// https://developers.google.com/youtube/player_parameters
    /// </summary>
    private static string AddYouTubeRel(string query)
    {
        // Add rel=0 unless specified in the original link
        if (query.Split('&').Any(q => q.StartsWith("rel=")) == false)
        {
            if (query.Length == 0)
                return "rel=0";
            else
                return query + "&rel=0";
        }

        return query;
    }

    private static readonly ReadOnlyCollection<string> hostsYouTube = new string[] {
        "youtube.com",
        "www.youtube.com",
        "youtu.be",
        "www.youtube-nocookie.com",
    }.AsReadOnly();

    private static string GetYouTubeId(string host, string path, ref string query)
    {
        if (host == "youtu.be")
        {
            return path.Substring(1);
        }

        var match = ReYouTubeQueryVideo().Match(query);
        if (match.Success)
        {
            //Remove from query
            query = query.Replace(match.Groups[0].Value, "").Trim('&').Replace("&&", "&");
            return match.Groups[2].Value;
        }

        match = ReYouTubePathId().Match(path);
        if (match.Success)
        {
            var id = match.Groups[1].Value;

            if (id == "videoseries")
                return null;

            return id;
        }

        return null;
    }

    [GeneratedRegex(@"(^|&)v=([^&]+)")]
    private static partial Regex ReYouTubeQueryVideo();

    [GeneratedRegex(@"(^|&)list=([^&]+)")]
    private static partial Regex ReYouTubeQueryList();

    [GeneratedRegex(@"/embed/([^/]+)$")]
    private static partial Regex ReYouTubePathId();

    private static string GetYouTubeList(string query)
    {
        var match = ReYouTubeQueryList().Match(query);
        if (match.Success)
        {
            return match.Groups[2].Value;
        }

        return null;
    }

}
