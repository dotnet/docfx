// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class VideoExtension : ITripleColonExtensionInfo
{
    public string Name => "video";

    public bool SelfClosing => true;

    public bool IsInline => true;

    public bool IsBlock => true;

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        var src = "";
        var title = "";
        var maxWidth = "";
        var thumbnail = "";
        var uploadDate = "";
        var duration = "";
        var type = "";

        foreach (var attribute in attributes)
        {
            var name = attribute.Key;
            var value = attribute.Value;
            switch (name)
            {
                case "title":
                    title = value;
                    break;
                case "max-width":
                    maxWidth = value;
                    break;
                case "source":
                    src = value;
                    break;
                case "thumbnail":
                    thumbnail = value;
                    break;
                case "upload-date":
                    uploadDate = value;
                    break;
                case "duration":
                    duration = value;
                    break;
                case "type":
                    type = value;
                    break;
                default:
                    logError($"Video reference '{src}' is invalid per the schema. Unexpected attribute: '{name}'.");
                    return false;
            }
        }

        if (string.IsNullOrEmpty(type))
        {
            type = "content";
        }
        if (string.IsNullOrEmpty(src))
        {
            logError("source is a required attribute. Please ensure you have specified a source attribute.");
            return false;
        }
        if (string.IsNullOrEmpty(thumbnail))
        {
            logError("thumbnail is a required attribute. Please ensure you have specified a thumbnail attribute.");
        }
        if (string.IsNullOrEmpty(uploadDate))
        {
            logError("upload-date is a required attribute. Please ensure you have specified a upload-date attribute.");
        }
        if (!src.Contains("channel9.msdn.com") &&
            !src.Contains("youtube.com/embed") &&
            !src.Contains("microsoft.com/en-us/videoplayer/embed"))
        {
            logWarning($"Video source, '{src}', should be from https://channel9.msdn.com, https://www.youtube.com/embed, or https://www.microsoft.com/en-us/videoplayer/embed");
        }
        if (src.Contains("channel9.msdn.com") && !src.Contains("/player"))
        {
            logWarning($"Your source from channel9.msdn.com does not end in '/player'. Please make sure you are correctly linking to the Channel 9 video player. ");
        }

        htmlAttributes = new HtmlAttributes();
        htmlAttributes.AddProperty("src", QuoteSectionNoteRender.FixUpLink(src));
        htmlAttributes.AddProperty("allowFullScreen", "true");
        htmlAttributes.AddProperty("frameBorder", "0");

        if (!string.IsNullOrEmpty(title))
        {
            htmlAttributes.AddProperty("title", title);
        }

        if (!string.IsNullOrEmpty(maxWidth))
        {
            if (!int.TryParse(maxWidth, out _))
            {
                logError($"Video reference '{src}' is invalid. 'max-width' must be a number.");
                return false;
            }
            htmlAttributes.AddProperty("style", $"max-width:{maxWidth}px;");
        }

        if (!string.IsNullOrEmpty(thumbnail))
        {
            htmlAttributes.AddProperty("thumbnail", thumbnail);
        }

        if (!string.IsNullOrEmpty(uploadDate))
        {
            htmlAttributes.AddProperty("upload-date", uploadDate);
        }

        if (!string.IsNullOrEmpty(duration))
        {
            htmlAttributes.AddProperty("duration", duration);
        }

        var id = GetHtmlId(markdownObject);
        if (type == "complex")
        {
            htmlAttributes.AddProperty("aria-describedby", id);
        }

        return true;
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning)
    {
        var tripleColonObj = (ITripleColon)markdownObject;

        if (!tripleColonObj.Attributes.TryGetValue("type", out var currentType))
        {
            currentType = "content";
        }
        else
        {
            if (tripleColonObj is Block)
            {
                renderer.WriteLine("<p>");
            }
        }

        if (currentType != "complex")
        {
            renderer.WriteLine("<div class=\"embeddedvideo\">");
            renderer.Write($"<iframe").WriteAttributes(markdownObject).WriteLine("></iframe>");
            renderer.WriteLine("</div>");
            if (tripleColonObj is ContainerBlock { LastChild: not null } block)
            {
                var inline = (block.LastChild as ParagraphBlock).Inline;
                renderer.WriteChildren(inline);
            }
        }
        else
        {
            if (currentType == "complex" && tripleColonObj.Count == 0)
            {
                logWarning("If type is \"complex\", then descriptive content is required. Please make sure you have descriptive content.");
                return false;
            }
            var htmlId = GetHtmlId(markdownObject);
            renderer.WriteLine("<div class=\"embeddedvideo\">");
            renderer.Write($"<iframe").WriteAttributes(markdownObject).WriteLine("></iframe>");
            renderer.WriteLine($"<div id=\"{htmlId}\" class=\"visually-hidden\">");
            renderer.WriteChildren(tripleColonObj as ContainerBlock);
            renderer.WriteLine("</div>");
        }

        if (tripleColonObj is Block)
        {
            renderer.WriteLine("</p>");
        }
        else
        {
            renderer.WriteChildren(tripleColonObj as ContainerInline);
        }

        return true;
    }

    public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
    {
        return true;
    }

    public static string GetHtmlId(MarkdownObject obj)
    {
        return $"{obj.Line}-{obj.Column}";
    }

    public static bool RequiresClosingTripleColon(IDictionary<string, string> attributes)
    {
        return attributes != null
               && attributes.TryGetValue("type", out string value)
               && value == "complex";
    }
}
