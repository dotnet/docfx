// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class ChromelessFormExtension : ITripleColonExtensionInfo
    {
        public string Name => "form";
        public bool SelfClosing => true;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }

        private Queue<string> submitTextArray = new Queue<string>();

        public bool Render(HtmlRenderer renderer, TripleColonBlock block)
        {
            return RenderDelegate != null
                ? RenderDelegate(renderer, block)
                : false;
        }

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError)
        {
            htmlAttributes = null;
            var model = string.Empty;
            var action = string.Empty;
            var submitText = string.Empty;
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
                        submitText = WebUtility.HtmlEncode(value);
                        break;
                    default:
                        logError($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (action == string.Empty)
            {
                logError($"Form action must be specified.");
                return false;
            }
            if (submitText == string.Empty)
            {
                logError($"Submit text must be specified.");
                return false;
            }

            htmlAttributes = new HtmlAttributes();
            if (model != string.Empty)
            {
                htmlAttributes.AddProperty("data-model", model);
            }
            htmlAttributes.AddProperty("data-action", action);
            htmlAttributes.AddClass("chromeless-form");

            submitTextArray.Enqueue(submitText);

            RenderDelegate = (renderer, obj) =>
            {
                var s = submitTextArray.Dequeue();

                renderer.Write("<form").WriteAttributes(obj).WriteLine(">");
                renderer.WriteLine("<div></div>");
                renderer.WriteLine($"<button class=\"button is-primary\" disabled=\"disabled\" type=\"submit\">{s}</button>");
                renderer.WriteLine("</form>");

                return true;
            };

            return true;
        }
        public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
        {
            return true;
        }
    }
}
