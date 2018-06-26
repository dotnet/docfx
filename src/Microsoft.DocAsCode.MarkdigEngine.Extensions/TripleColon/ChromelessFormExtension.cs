// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
	using Markdig.Renderers;
	using Markdig.Renderers.Html;
	using Markdig.Syntax;
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class ChromelessFormExtension : ITripleColonExtensionInfo
    {
		public string Name => "form";
		public bool SelfClosing => true;
		public Func<HtmlRenderer, TripleColonBlock, bool> Render { get; private set; }

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
						submitText = value;
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

			Render = (renderer, obj) =>
			{
				renderer.Write("<form").WriteAttributes(obj).WriteLine(">");

				if (String.IsNullOrEmpty(model))
				{
					renderer.WriteLine($"<button type=\"submit\">{submitText}</button>");
				}
				else
				{
					renderer.WriteLine(@"<fieldset disabled=""disabled"">");
					renderer.WriteLine("<div></div>");
					renderer.WriteLine($"<button type=\"submit\">{submitText}</button>");
					renderer.WriteLine("</fieldset>");
				}

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
