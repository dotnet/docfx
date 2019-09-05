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
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    public class ImageExtension : ITripleColonExtensionInfo
    {
        public string Name => "image";
        public bool SelfClosing => true;
        public bool EndingTripleColons => true;
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
            foreach (var attribute in attributes)
            {
                var name = attribute.Key;
                var value = attribute.Value;
                switch (name)
                {
                    case "alt-text":
                        alt = value;
                        break;
                    case "source":
                        src = value;
                        break;
                    default:
                        logError($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (string.IsNullOrEmpty(src))
            {
                logError($"source is a required attribute. Please ensure you have specified a source attribute");
            }
            if (string.IsNullOrEmpty(alt))
            {
                logError($"alt-text is a required attribute. Please ensure you have specified an alt-text attribute.");
            }
            if (string.IsNullOrEmpty(alt) || string.IsNullOrEmpty(src))
            {
                return false;
            }
            var id = GetHtmlId(processor.LineIndex, processor.Column);
            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("src", src);
            htmlAttributes.AddProperty("alt", alt);
            htmlAttributes.AddProperty("aria-describedby", id);

            RenderDelegate = (renderer, obj) =>
            {
                renderer.Write("<img").WriteAttributes(obj).WriteLine(">");
                renderer.WriteLine($"<div id=\"{id}\" class=\"visually-hidden\">");
                renderer.WriteChildren(obj);
                renderer.WriteLine("</div>");

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
}
