// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class ImageExtension : ITripleColonExtensionInfo
{
    private readonly MarkdownContext _context;

    public string Name => "image";

    public bool SelfClosing => true;

    public bool IsInline => true;

    public bool IsBlock => true;

    public ImageExtension(MarkdownContext context)
    {
        _context = context;
    }

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        var src = "";
        var alt = "";
        var type = "";
        foreach (var attribute in attributes)
        {
            var name = attribute.Key;
            var value = attribute.Value;
            switch (name)
            {
                case "alt-text":
                    alt = value;
                    break;
                case "type":
                    type = value;
                    break;
                case "loc-scope":
                    var loc_scope = value;
                    break;
                case "source":
                    src = value;
                    break;
                case "border":
                    break;
                case "lightbox":
                    break;
                case "link":
                    break;
                default:
                    logError($"Image reference '{src}' is invalid per the schema. Unexpected attribute: '{name}'.");
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
        }
        if (string.IsNullOrEmpty(alt) && type != "icon")
        {
            logError("alt-text is a required attribute. Please ensure you have specified an alt-text attribute.");
        }

        // add loc scope missing/invalid validation here
        if ((string.IsNullOrEmpty(alt) && type != "icon") || string.IsNullOrEmpty(src))
        {
            return false;
        }

        return true;
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject obj, Action<string> logWarning)
    {
        var tripleColonObj = (ITripleColon)obj;

        if (!tripleColonObj.Attributes.TryGetValue("type", out var currentType))
        {
            currentType = "content";
        }
        tripleColonObj.Attributes.TryGetValue("lightbox", out var currentLightbox); // it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("border", out var currentBorderStr); // it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("link", out var currentLink); // it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("alt-text", out var alt); // it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("source", out var src); // it's okay if this is null

        var htmlAttributes = new HtmlAttributes();

        htmlAttributes.AddProperty("src", _context.GetImageLink(src, obj, alt));

        if (currentType == "icon")
        {
            htmlAttributes.AddProperty("role", "presentation");
        }
        else
        {
            htmlAttributes.AddProperty("alt", alt);
        }
        var htmlId = GetHtmlId(obj);
        if (currentType == "complex")
        {
            htmlAttributes.AddProperty("aria-describedby", htmlId);
        }

        if (!bool.TryParse(currentBorderStr, out var currentBorder))
        {
            currentBorder = currentType != "icon";
        }

        if (currentBorder)
        {
            if (tripleColonObj is Block)
            {
                renderer.WriteLine("<p class=\"mx-imgBorder\">");
            }
            else
            {
                renderer.WriteLine("<span class=\"mx-imgBorder\">");
            }
        }
        else
        {
            if (tripleColonObj is Block)
            {
                renderer.WriteLine("<p>");
            }
        }
        if (!string.IsNullOrEmpty(currentLink))
        {
            var linkHtmlAttributes = new HtmlAttributes();
            currentLink = _context.GetLink(currentLink, obj);
            linkHtmlAttributes.AddProperty("href", $"{currentLink}");
            renderer.Write("<a").WriteAttributes(linkHtmlAttributes).WriteLine(">");
        }
        else if (!string.IsNullOrEmpty(currentLightbox))
        {
            var lightboxHtmlAttributes = new HtmlAttributes();
            var path = _context.GetLink(currentLightbox, obj);
            lightboxHtmlAttributes.AddProperty("href", $"{path}#lightbox");
            lightboxHtmlAttributes.AddProperty("data-linktype", $"relative-path");
            renderer.Write("<a").WriteAttributes(lightboxHtmlAttributes).WriteLine(">");
        }
        if (currentType != "complex")
        {
            renderer.Write("<img").WriteAttributes(htmlAttributes).WriteLine(">");

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
            renderer.Write("<img").WriteAttributes(htmlAttributes).WriteLine(">");
            renderer.WriteLine($"<div id=\"{htmlId}\" class=\"visually-hidden\"><p>");
            renderer.Write(tripleColonObj.Body);
            renderer.WriteLine("</p></div>");
        }
        if (!string.IsNullOrEmpty(currentLightbox) || !string.IsNullOrEmpty(currentLink))
        {
            renderer.WriteLine($"</a>");
        }
        if (tripleColonObj is Block)
        {
            renderer.WriteLine("</p>");
        }
        else
        {
            if (currentBorder)
            {
                renderer.WriteLine("</span>");
            }

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
