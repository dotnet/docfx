// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using System;
    using System.Collections.Generic;

    public class ImageExtension : ITripleColonExtensionInfo
    {
        private readonly MarkdownContext _context;

        public string Name => "image";
        public bool SelfClosing => true;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }

        public ImageExtension(MarkdownContext context)
        {
            _context = context;
        }

        public bool Render(HtmlRenderer renderer, TripleColonBlock block)
        {
            return RenderDelegate != null
                ? RenderDelegate(renderer, block)
                : false;
        }

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, TripleColonBlock block)
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

            if(string.IsNullOrEmpty(type))
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
            if ((string.IsNullOrEmpty(alt) && type != "icon") || string.IsNullOrEmpty(src))
            {
                return false;
            }
            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("src", _context.GetLink(src, block));

            if (type == "icon")
            {
                htmlAttributes.AddProperty("role", "presentation");
            } else
            {
                htmlAttributes.AddProperty("alt", alt);
            }
            var id = GetHtmlId(block);
            if(type == "complex") htmlAttributes.AddProperty("aria-describedby", id);

            RenderDelegate = (renderer, obj) =>
            {
                var currentType = string.Empty;
                var currentLightbox = string.Empty;
                var currentBorderStr = string.Empty;
                var currentBorder = true;
                var currentLink = string.Empty;
                if(!obj.Attributes.TryGetValue("type", out currentType))
                {
                    currentType = "content";
                }
                obj.Attributes.TryGetValue("lightbox", out currentLightbox); //it's okay if this is null
                obj.Attributes.TryGetValue("border", out currentBorderStr); //it's okay if this is null
                obj.Attributes.TryGetValue("link", out currentLink); //it's okay if this is null
                if (!bool.TryParse(currentBorderStr, out currentBorder))
                {
                    if(currentType == "icon")
                    {
                        currentBorder = false;
                    } else
                    {
                        currentBorder = true;
                    }
                }

                if(!string.IsNullOrEmpty(currentLink))
                {
                    var linkHtmlAttributes = new HtmlAttributes();
                    currentLink = _context.GetLink(currentLink, obj);
                    linkHtmlAttributes.AddProperty("href", $"{currentLink}");
                    renderer.Write("<a").WriteAttributes(linkHtmlAttributes).WriteLine(">");
                } else if (!string.IsNullOrEmpty(currentLightbox))
                {
                    var lightboxHtmlAttributes = new HtmlAttributes();
                    var path = _context.GetLink(currentLightbox, obj);
                    lightboxHtmlAttributes.AddProperty("href", $"{path}#lightbox");
                    lightboxHtmlAttributes.AddProperty("data-linktype", $"relative-path");
                    renderer.Write("<a").WriteAttributes(lightboxHtmlAttributes).WriteLine(">");
                }
                if(currentBorder)
                {
                    renderer.WriteLine("<div class=\"mx-imgBorder\"><p>");
                }
                if(currentType != "complex")
                {
                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                } else
                {
                    if (currentType == "complex" && obj.Count == 0)
                    {
                        logWarning("If type is \"complex\", then descriptive content is required. Please make sure you have descriptive content.");
                        return false;
                    }
                    var htmlId = GetHtmlId(obj);
                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                    renderer.WriteLine($"<div id=\"{htmlId}\" class=\"visually-hidden\">");
                    renderer.WriteChildren(obj);
                    renderer.WriteLine("</div>");
                }

                if (currentBorder)
                {
                    renderer.WriteLine("</p></div>");
                }
                if (!string.IsNullOrEmpty(currentLightbox) || !string.IsNullOrEmpty(currentLink))
                {
                    renderer.WriteLine($"</a>");
                }

                return true;
            };

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
            if(attributes != null
               && attributes.ContainsKey("type")
               && attributes["type"] == "complex")
            {
                return true;
            } else
            {
                return false;
            }
        }
    }
}
