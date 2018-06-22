
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using static Microsoft.DocAsCode.MarkdigEngine.Extensions.MarkdownContext;

    public class ZoneExtension : ITripleColonExtensionInfo
    {
        private static readonly Regex pivotRegex = new Regex(@"^(?:\s*[a-z0-9-]\s*)*$");
        public string Name { get; } = "zone";
        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, LogActionDelegate logError)
        {
            htmlAttributes = null;
            var target = "docs";
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
                            logError("invalid-zone", $"Invalid zone. Unexpected target \"{value}\". Permitted targets are \"docs\", \"chromeless\" or \"pdf\".");
                            return false;
                        }
                        target = value;
                        break;
                    case "pivot":
                        if (!pivotRegex.IsMatch(value))
                        {
                            logError("invalid-zone", $"Invalid zone pivot \"{value}\". Pivot must be a space-delimited list of pivot names. Pivot names must be lower-case and contain only letters, numbers and dashes.");
                            return false;
                        }
                        pivot = value;
                        break;
                    default:
                        logError("invalid-zone", $"Invalid zone. Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (target == "pdf" && pivot != string.Empty)
            {
                logError("invalid-zone", $"Invalid zone. Pivot not permitted in pdf target.");
                return false;
            }

            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("data-zone", target);
            if (pivot != string.Empty)
            {
                htmlAttributes.AddProperty("data-pivot", pivot);
            }
            return true;
        }
        public bool TryValidateAncestry(ContainerBlock container, LogActionDelegate logError)
        {
            while (container != null)
            {
                if (container is TripleColonBlock && ((TripleColonBlock)container).Extension.Name == this.Name)
                {
                    logError("invalid-zone", "Zones cannot be nested.");
                    return false;
                }
                container = container.Parent;
            }
            return true;
        }
    }
}
