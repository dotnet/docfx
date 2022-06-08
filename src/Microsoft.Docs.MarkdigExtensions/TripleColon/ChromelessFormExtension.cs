// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Net;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class ChromelessFormExtension : ITripleColonExtensionInfo
{
    public string Name => "form";

    public bool SelfClosing => true;

    public bool IsInline => false;

    public bool IsBlock => true;

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        var model = "";
        var action = "";
        var submitText = "";
        foreach (var attribute in attributes)
        {
            var name = attribute.Key;
            var value = attribute.Value;
            switch (name)
            {
                case "model":
                    model = value;
                    break;
                case "action":
                    action = value;
                    break;
                case "submittext":
                    submitText = value;
                    break;
                default:
                    logError($"Unexpected attribute \"{name}\".");
                    return false;
            }
        }

        if (string.IsNullOrEmpty(action))
        {
            logError("Form action must be specified.");
            return false;
        }
        if (string.IsNullOrEmpty(submitText))
        {
            logError("Submit text must be specified.");
            return false;
        }

        htmlAttributes = new HtmlAttributes();
        if (!string.IsNullOrEmpty(model))
        {
            htmlAttributes.AddProperty("data-model", model);
        }
        htmlAttributes.AddProperty("data-action", action);
        htmlAttributes.AddClass("chromeless-form");

        return true;
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning)
    {
        var block = (TripleColonBlock)markdownObject;
        block.Attributes.TryGetValue("submitText", out var submitText);

        renderer.Write("<form").WriteAttributes(block).WriteLine(">");
        renderer.WriteLine("<div></div>");
        renderer.WriteLine($"<button class=\"button is-primary\" disabled=\"disabled\" type=\"submit\">{WebUtility.HtmlEncode(submitText)}</button>");
        renderer.WriteLine("</form>");

        return true;
    }

    public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
    {
        return true;
    }
}
