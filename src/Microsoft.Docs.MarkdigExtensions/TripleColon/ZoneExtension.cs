// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class ZoneExtension : ITripleColonExtensionInfo
{
    private static readonly Regex s_pivotRegex = new(@"^\s*(?:[a-z0-9-]+)(?:\s*,\s*[a-z0-9-]+)*\s*$");
    private static readonly Regex s_pivotReplaceCommasRegex = new(@"\s*,\s*");

    public string Name => "zone";

    public bool SelfClosing => false;

    public bool IsInline => false;

    public bool IsBlock => true;

    public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning)
    {
        return false;
    }

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        var target = "";
        var pivot = "";
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
                    if (!s_pivotRegex.IsMatch(value))
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

        if (string.IsNullOrEmpty(target) && string.IsNullOrEmpty(pivot))
        {
            logError("Either target or pivot must be specified.");
            return false;
        }
        if (target == "pdf" && !string.IsNullOrEmpty(pivot))
        {
            logError("Pivot not permitted on pdf target.");
            return false;
        }

        htmlAttributes = new HtmlAttributes();
        htmlAttributes.AddClass("zone");
        if (!string.IsNullOrEmpty(target))
        {
            htmlAttributes.AddClass("has-target");
            htmlAttributes.AddProperty("data-target", target);
        }
        if (!string.IsNullOrEmpty(pivot))
        {
            htmlAttributes.AddClass("has-pivot");
            htmlAttributes.AddProperty("data-pivot", pivot.Trim().ReplaceRegex(s_pivotReplaceCommasRegex, " "));
        }
        return true;
    }

    public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
    {
        while (container != null)
        {
            if (container is TripleColonBlock && ((TripleColonBlock)container).Extension.Name == Name)
            {
                logError("Zones cannot be nested.");
                return false;
            }
            container = container.Parent;
        }
        return true;
    }
}
