// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class ImageExtension : ITripleColonExtensionInfo
{
    private readonly MarkdownContext _context;

    public string Name => "image";
    public bool SelfClosing => true;

    public ImageExtension(MarkdownContext context)
    {
        _context = context;
    }

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        renderProperties = new Dictionary<string, string>();
        var src = string.Empty;
        var alt = string.Empty;
        var type = string.Empty;
        var loc_scope = string.Empty;
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
                    loc_scope = value;
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
        htmlAttributes = new HtmlAttributes();

        // alt is allowed to be empty for icon type image
        if (string.IsNullOrEmpty(alt) && type == "icon")
            htmlAttributes.AddProperty("src", _context.GetLink(src, markdownObject));
        else
            htmlAttributes.AddProperty("src", _context.GetImageLink(src, markdownObject, alt));

        if (type == "icon")
        {
            htmlAttributes.AddProperty("role", "presentation");
        }
        else
        {
            htmlAttributes.AddProperty("alt", alt);
        }
        var id = GetHtmlId(markdownObject);
        if (type == "complex") htmlAttributes.AddProperty("aria-describedby", id);

        return true;
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject obj, Action<string> logWarning)
    {
        var tripleColonObj = (ITripleColon)obj;

        if (!tripleColonObj.Attributes.TryGetValue("type", out var currentType))
        {
            currentType = "content";
        }
        tripleColonObj.Attributes.TryGetValue("lightbox", out var currentLightbox); //it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("border", out var currentBorderStr); //it's okay if this is null
        tripleColonObj.Attributes.TryGetValue("link", out var currentLink); //it's okay if this is null
        if (!bool.TryParse(currentBorderStr, out var currentBorder))
        {
            if (currentType == "icon")
            {
                currentBorder = false;
            }
            else
            {
                currentBorder = true;
            }
        }

        if (currentBorder)
        {
            if (tripleColonObj is Block)
            {
                renderer.WriteLine("<p class=\"mx-imgBorder\">");
            } else
            {
                renderer.WriteLine("<span class=\"mx-imgBorder\">");
            }

        }
        else
        {
            if(tripleColonObj is Block) renderer.WriteLine("<p>");
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
            renderer.Write("<img").WriteAttributes(obj).WriteLine(">");

            if(tripleColonObj is ContainerBlock
                && (tripleColonObj as ContainerBlock).LastChild != null)
            {
                var inline = ((tripleColonObj as ContainerBlock).LastChild as ParagraphBlock).Inline;
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
            var htmlId = GetHtmlId(obj);
            renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
            renderer.WriteLine($"<div id=\"{htmlId}\" class=\"visually-hidden\">");
            renderer.WriteChildren(tripleColonObj as ContainerBlock);
            renderer.WriteLine("</div>");
        }
        if (!string.IsNullOrEmpty(currentLightbox) || !string.IsNullOrEmpty(currentLink))
        {
            renderer.WriteLine($"</a>");
        }
        if (tripleColonObj is Block)
        {
            renderer.WriteLine("</p>");
        } else
        {
            if(currentBorder) renderer.WriteLine("</span>");
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
        if (attributes != null
           && attributes.ContainsKey("type")
           && attributes["type"] == "complex")
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
