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
    using System.Diagnostics;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public class ImageExtension : ITripleColonExtensionInfo
    {
        public string Name => "image";
        public bool SelfClosing => true;
        public bool EndingTripleColons { get; set; } = false;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }
        private List<ImageProperties> imagesList = new List<ImageProperties>();

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
                    case "source":
                        src = value;
                        break;
                    default:
                        logError($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if(type == "complex")
            {
                EndingTripleColons = true;
            } else
            {
                EndingTripleColons = false;
            }

            if (string.IsNullOrEmpty(src))
            {
                logError($"source is a required attribute. Please ensure you have specified a source attribute.");
            }
            if (string.IsNullOrEmpty(type))
            {
                logError($"type is a required attribute. Please ensure you have specified a type attribute. Options (\"comples\", \"content\", \"icon\")");
            }
            if (string.IsNullOrEmpty(alt) && type != "icon")
            {
                logError($"alt-text is a required attribute. Please ensure you have specified an alt-text attribute.");
            }
            if ((string.IsNullOrEmpty(alt) && type != "icon") || string.IsNullOrEmpty(src))
            {
                return false;
            }
            var id = GetHtmlId(processor.LineIndex, processor.Column);
            var image = new ImageProperties
            {
                id = id,
                type = type
            };
            imagesList.Add(image);
            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("src", src);
            if (type == "icon")
            {
                htmlAttributes.AddProperty("role", "presentation");
            } else
            {
                htmlAttributes.AddProperty("alt", alt);
            }
            if(type == "complex") htmlAttributes.AddProperty("aria-describedby", id);

            var imageCount = 0;
            RenderDelegate = (renderer, obj) =>
            {
                image = imagesList[imageCount];
                imageCount++;
                //if obj.Count == 0, this signifies that there is no long description for the image.
                if(obj.Count == 0)
                {
                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                } else
                {
                    if(type == "complex" && obj.Count == 0)
                    {
                        logError($"If type is \"complex\", then descriptive content is required. Please make sure you have descriptive content.");
                        return false;
                    }

                    renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                    renderer.WriteLine($"<div id=\"{image.id}\" class=\"visually-hidden\">");
                    renderer.WriteChildren(obj);
                    renderer.WriteLine("</div>");
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
            using (var md5 = MD5.Create())
            {
                var id = $"{InclusionContext.File}-{line}-{column}";
                var fileBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(id));

                return new Guid(fileBytes).ToString("N").Substring(0, 5);
            }
        }
    }

    public class ImageProperties
    {
        public string id { get; set; }
        public string type { get; set; }
    }
}
