// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    public class ImageExtension : ITripleColonExtensionInfo
    {
        public string Name => "image";
        public bool SelfClosing => true;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }

        public bool Render(HtmlRenderer renderer, TripleColonBlock block)
        {
            return RenderDelegate != null
                ? RenderDelegate(renderer, block)
                : false;
        }

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, BlockProcessor processor)
        {
            htmlAttributes = null;
            renderProperties = new Dictionary<string, string>();
            var src = string.Empty;
            var alt = string.Empty;
            var type = string.Empty;
            var loc_scope = string.Empty;
            var border = true;
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
                        if(!bool.TryParse(value, out border))
                        {
                            border = true;
                        }
                        break;
                    case "lightbox":
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
            htmlAttributes.AddProperty("src", src);

            if (type == "icon")
            {
                htmlAttributes.AddProperty("role", "presentation");
            } else
            {
                htmlAttributes.AddProperty("alt", alt);
            }
            var id = GetHtmlId(processor.LineIndex, processor.Column);
            if(type == "complex") htmlAttributes.AddProperty("aria-describedby", id);

            RenderDelegate = (renderer, obj) =>
            {
                var currentLightbox = string.Empty;
                var currentBorderStr = string.Empty;
                var currentBorder = true;
                obj.Attributes.TryGetValue("lightbox", out currentLightbox); //it's okay if this is null
                obj.Attributes.TryGetValue("border", out currentBorderStr); //it's okay if this is null
                if(!bool.TryParse(currentBorderStr, out currentBorder))
                {
                    currentBorder = true;
                }

                if (!string.IsNullOrEmpty(currentLightbox))
                {
                    var lighboxHtmlAttributes = new HtmlAttributes();
                    lighboxHtmlAttributes.AddProperty("href", $"{currentLightbox}#lightbox");
                    lighboxHtmlAttributes.AddProperty("data-linktype", $"relative-path");
                    //renderer.WriteLine($"<a href=\"{currentLightbox}#lightbox\" data-linktype=\"relative-path\">");
                    renderer.Write("<a").WriteAttributes(lighboxHtmlAttributes).WriteLine(">");
                }
                if(currentBorder)
                {
                    renderer.WriteLine("<div class=\"mx-imgBorder\"><p>");
                }
                //if obj.Count == 0, this signifies that there is no long description for the image.
                if(obj.Count == 0)
                {
                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                } else
                {
                    if(type == "complex" && obj.Count == 0)
                    {
                        logError("If type is \"complex\", then descriptive content is required. Please make sure you have descriptive content.");
                        return false;
                    }
                    var htmlId = GetHtmlId(obj.Line, obj.Column);
                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                    renderer.WriteLine($"<div id=\"{htmlId}\" class=\"visually-hidden\">");
                    renderer.WriteChildren(obj);
                    renderer.WriteLine("</div>");
                }

                if (currentBorder)
                {
                    renderer.WriteLine("</p></div>");
                }
                if (!string.IsNullOrEmpty(currentLightbox))
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

        public static string GetHtmlId(int line, int column)
        {
            using var md5 = MD5.Create();
            var id = $"{InclusionContext.File}-{line}-{column}";
            var fileBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(id));

            return new Guid(fileBytes).ToString("N").Substring(0, 5);
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

    public class ImageProperties
    {
        public string id { get; set; }
        public string type { get; set; }
    }
}
