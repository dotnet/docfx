// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class VideoExtension : ITripleColonExtensionInfo
{
    public string Name => "video";
    public bool SelfClosing => true;

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        renderProperties = new Dictionary<string, string>();
        var src = string.Empty;
        var title = string.Empty;
        var maxWidth = string.Empty;
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
                default:
                    logError($"Video reference '{src}' is invalid per the schema. Unexpected attribute: '{name}'.");
                    return false;
            }
        }

        if (string.IsNullOrEmpty(src))
        {
            logError("source is a required attribute. Please ensure you have specified a source attribute.");
            return false;
        }
        if(!src.Contains("channel9.msdn.com") &&
            !src.Contains("youtube.com/embed") &&
            !src.Contains("microsoft.com/en-us/videoplayer/embed"))
        {
            logWarning($"Video source, '{src}', should be from https://channel9.msdn.com, https://www.youtube.com/embed, or https://www.microsoft.com/en-us/videoplayer/embed");
        }
        if(src.Contains("channel9.msdn.com") && !src.Contains("/player"))
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
            int number;
            if(!int.TryParse(maxWidth, out number))
            {
                logError($"Video reference '{src}' is invalid. 'max-width' must be a number.");
                return false;
            }
            htmlAttributes.AddProperty("style", $"max-width:{maxWidth}px;");
        }

        return true;
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning)
    {
        renderer.WriteLine("<div class=\"embeddedvideo\">");
        renderer.Write($"<iframe").WriteAttributes(markdownObject).WriteLine(">");
        renderer.WriteLine("</div>");

        return true;
    }

    public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
    {
        return true;
    }
}
