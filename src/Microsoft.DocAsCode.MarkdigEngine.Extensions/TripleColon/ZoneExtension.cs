// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
	using Markdig.Renderers;
	using Markdig.Renderers.Html;
	using Markdig.Syntax;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	public class ZoneExtension : ITripleColonExtensionInfo
    {
        private static readonly Regex pivotRegex = new Regex(@"^\s*(?:[a-z0-9-]+)(?:\s*,\s*[a-z0-9-]+)*\s*$");
        private static readonly Regex pivotReplaceCommasRegex = new Regex(@"\s*,\s*");
        public string Name => "zone";
		public bool SelfClosing => false;

		public bool Render(HtmlRenderer renderer, TripleColonBlock block)
		{
			return false;
		}

		public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError)
        {
            htmlAttributes = null;
			renderProperties = null;
            var target = string.Empty;
            var pivot = string.Empty;
            foreach (var attribute in attributes)
            {
                var name = attribute.Key;
                var value = attribute.Value;
                switch (name)
                {
                    case "target":
                        if (value != "docs" && value != "chromeless" && value != "pdf")
                        {
                            logError($"Unexpected target \"{value}\". Permitted targets are \"docs\", \"chromeless\" or \"pdf\".");
                            return false;
                        }
                        target = value;
                        break;
                    case "pivot":
                        if (!pivotRegex.IsMatch(value))
                        {
                            logError($"Invalid pivot \"{value}\". Pivot must be a comma-delimited list of pivot names. Pivot names must be lower-case and contain only letters, numbers or dashes.");
                            return false;
                        }
                        pivot = value;
                        break;
                    default:
                        logError($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (target == string.Empty && pivot == string.Empty)
            {
                logError($"Either target or privot must be specified.");
                return false;
            }
            if (target == "pdf" && pivot != string.Empty)
            {
                logError($"Pivot not permitted on pdf target.");
                return false;
            }

            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddClass("zone");
            if (target != string.Empty)
            {
                htmlAttributes.AddClass("has-target");
                htmlAttributes.AddProperty("data-target", target);
            }
            if (pivot != string.Empty)
            {
                htmlAttributes.AddClass("has-pivot");
                htmlAttributes.AddProperty("data-pivot", pivot.Trim().ReplaceRegex(pivotReplaceCommasRegex, " "));
            }
            return true;
        }
        public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
        {
            while (container != null)
            {
                if (container is TripleColonBlock && ((TripleColonBlock)container).Extension.Name == this.Name)
                {
                    logError("Zones cannot be nested.");
                    return false;
                }
                container = container.Parent;
            }
            return true;
        }
    }
}
