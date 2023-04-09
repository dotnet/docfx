// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal class XmlComment
{
    private const string idSelector = @"((?![0-9])[\w_])+[\w\(\)\.\{\}\[\]\|\*\^~#@!`,_<>:]*";
    private static readonly Regex CommentIdRegex = new(@"^(?<type>N|T|M|P|F|E|Overload):(?<id>" + idSelector + ")$", RegexOptions.Compiled);
    private static readonly Regex RegionRegex = new(@"^\s*#region\s*(.*)$");
    private static readonly Regex XmlRegionRegex = new(@"^\s*<!--\s*<([^/\s].*)>\s*-->$");
    private static readonly Regex EndRegionRegex = new(@"^\s*#endregion\s*.*$");
    private static readonly Regex XmlEndRegionRegex = new(@"^\s*<!--\s*</(.*)>\s*-->$");

    private readonly XmlCommentParserContext _context;

    public string Summary { get; private set; }

    public string Remarks { get; private set; }

    public string Returns { get; private set; }

    public List<ExceptionInfo> Exceptions { get; private set; }

    public List<LinkInfo> SeeAlsos { get; private set; }

    public List<string> Examples { get; private set; }

    public Dictionary<string, string> Parameters { get; private set; }

    public Dictionary<string, string> TypeParameters { get; private set; }

    private XmlComment(string xml, XmlCommentParserContext context)
    {
        // Treat <doc> as <member>
        if (xml.StartsWith("<doc>") && xml.EndsWith("</doc>"))
        {
            var innerXml = xml.Substring(5, xml.Length - 11);
            var innerXmlTrim = innerXml.Trim();

            // Workaround external XML doc not wrapped in summary tag: https://github.com/dotnet/roslyn/pull/66668
            if (innerXmlTrim.StartsWith('<') && innerXmlTrim.EndsWith('>'))
                xml = $"<member>{innerXml}</member>";
            else
                xml = $"<member><summary>{innerXml}</summary></member>";
        }

        // Workaround: https://github.com/dotnet/roslyn/pull/66668
        if (!xml.StartsWith("<member", StringComparison.Ordinal) && !xml.EndsWith("</member>", StringComparison.Ordinal))
        {
            xml = $"<member>{xml}</member>";
        }

        // Transform triple slash comment
        var doc = XmlCommentTransformer.Transform(xml);

        _context = context;

        ResolveLangword(doc);
        ResolveCrefLink(doc, "//seealso[@cref]", context.AddReferenceDelegate);
        ResolveCrefLink(doc, "//see[@cref]", context.AddReferenceDelegate);
        ResolveCrefLink(doc, "//exception[@cref]", context.AddReferenceDelegate);

        ResolveCode(doc, context);

        var nav = doc.CreateNavigator();
        Summary = GetSingleNodeValue(nav, "/member/summary");
        Remarks = GetSingleNodeValue(nav, "/member/remarks");
        Returns = GetSingleNodeValue(nav, "/member/returns");

        Exceptions = ToListNullOnEmpty(GetMulitpleCrefInfo(nav, "/member/exception"));
        SeeAlsos = ToListNullOnEmpty(GetMultipleLinkInfo(nav, "/member/seealso"));
        Examples = GetMultipleExampleNodes(nav, "/member/example").ToList();
        Parameters = GetListContent(nav, "/member/param", "parameter", context);
        TypeParameters = GetListContent(nav, "/member/typeparam", "type parameter", context);

        // Nulls and empty list are treated differently in overwrite files:
        //   null values can be replaced, but empty list are merged by merge key
        static List<T> ToListNullOnEmpty<T>(IEnumerable<T> items)
        {
            var list = items.ToList();
            return list.Count == 0 ? null : list;
        }
    }

    public static XmlComment Parse(string xml, XmlCommentParserContext context = null)
    {
        if (string.IsNullOrEmpty(xml)) return null;

        // Quick turnaround for badly formed XML comment
        if (xml.StartsWith("<!-- Badly formed XML comment ignored for member ", StringComparison.Ordinal))
        {
            Logger.LogWarning($"Invalid triple slash comment is ignored: {xml}");
            return null;
        }
        try
        {
            return new XmlComment(xml, context ?? new());
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public string GetParameter(string name)
    {
        return Parameters.TryGetValue(name, out var value) ? value : null;
    }

    public string GetTypeParameter(string name)
    {
        return TypeParameters.TryGetValue(name, out var value) ? value : null;
    }

    private void ResolveCode(XDocument doc, XmlCommentParserContext context)
    {
        foreach (var node in doc.XPathSelectElements("//code").ToList())
        {
            if (node.Attribute("data-inline") is { } inlineAttribute)
            {
                inlineAttribute.Remove();
                continue;
            }

            var indent = ((IXmlLineInfo)node).LinePosition - 2;
            var (lang, value) = ResolveCodeSource(node, context);
            value = TrimEachLine(value ?? node.Value, new(' ', indent));
            var code = new XElement("code", value);
            code.SetAttributeValue("class", $"lang-{lang ?? "csharp"}");
            node.ReplaceWith(new XElement("pre", code));
        }
    }

    private (string lang, string code) ResolveCodeSource(XElement node, XmlCommentParserContext context)
    {
        var source = node.Attribute("source")?.Value;
        if (string.IsNullOrEmpty(source))
            return default;

        var lang = Path.GetExtension(source).TrimStart('.').ToLowerInvariant();

        var code = context.ResolveCode?.Invoke(source);
        if (code is null)
            return (lang, null);

        var region = node.Attribute("region")?.Value;
        if (region is null)
            return (lang, code);

        var (regionRegex, endRegionRegex) = GetRegionRegex(source);

        var builder = new StringBuilder();
        var regionCount = 0;

        foreach (var line in ReadLines(code))
        {
            if (!string.IsNullOrEmpty(region))
            {
                var match = regionRegex.Match(line);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (name == region)
                    {
                        ++regionCount;
                        continue;
                    }
                    else if (regionCount > 0)
                    {
                        ++regionCount;
                    }
                }
                else if (regionCount > 0 && endRegionRegex.IsMatch(line))
                {
                    --regionCount;
                    if (regionCount == 0)
                    {
                        break;
                    }
                }

                if (regionCount > 0)
                {
                    builder.AppendLine(line);
                }
            }
            else
            {
                builder.AppendLine(line);
            }
        }

        return (lang, builder.ToString());
    }

    private static IEnumerable<string> ReadLines(string text)
    {
        string line;
        using var sr = new StringReader(text);
        while ((line = sr.ReadLine()) != null)
        {
            yield return line;
        }
    }

    private Dictionary<string, string> GetListContent(XPathNavigator navigator, string xpath, string contentType, XmlCommentParserContext context)
    {
        var iterator = navigator.Select(xpath);
        var result = new Dictionary<string, string>();
        if (iterator == null)
        {
            return result;
        }
        foreach (XPathNavigator nav in iterator)
        {
            string name = nav.GetAttribute("name", string.Empty);
            string description = GetXmlValue(nav);
            if (!string.IsNullOrEmpty(name))
            {
                if (result.ContainsKey(name))
                {
                    string path = context.Source?.Remote != null ? Path.Combine(EnvironmentContext.BaseDirectory, context.Source.Remote.RelativePath) : context.Source?.Path;
                    Logger.LogWarning($"Duplicate {contentType} '{name}' found in comments, the latter one is ignored.", file: StringExtension.ToDisplayPath(path), line: context.Source?.StartLine.ToString());
                }
                else
                {
                    result.Add(name, description);
                }
            }
        }

        return result;
    }

    private static (Regex, Regex) GetRegionRegex(String source)
    {
        var ext = Path.GetExtension(source);
        switch (ext.ToUpper())
        {
            case ".XML":
            case ".XAML":
            case ".HTML":
            case ".CSHTML":
            case ".VBHTML":
                return (XmlRegionRegex, XmlEndRegionRegex);
        }

        return (RegionRegex, EndRegionRegex);
    }

    private void ResolveLangword(XNode node)
    {
        foreach (var item in node.XPathSelectElements("//see[@langword]").ToList())
        {
            var langword = item.Attribute("langword").Value;
            if (SymbolUrlResolver.GetLangwordUrl(langword) is { } href)
            {
                var a = new XElement("a", langword);
                a.SetAttributeValue("href", href);
                item.ReplaceWith(a);
            }
            else
            {
                var code = new XElement("code", langword);
                code.SetAttributeValue("data-inline", "true");
                item.ReplaceWith(code);
            }
        }
    }

    private void ResolveCrefLink(XNode node, string nodeSelector, Action<string, string> addReference)
    {
        if (node == null || string.IsNullOrEmpty(nodeSelector))
        {
            return;
        }

        try
        {
            var nodes = node.XPathSelectElements(nodeSelector + "[@cref]").ToList();
            foreach (var item in nodes)
            {
                var cref = item.Attribute("cref").Value;
                var success = false;

                // Strict check is needed as value could be an invalid href,
                // e.g. !:Dictionary&lt;TKey, string&gt; when user manually changed the intellisensed generic type
                var match = CommentIdRegex.Match(cref);
                if (match.Success)
                {
                    var id = match.Groups["id"].Value;
                    var type = match.Groups["type"].Value;

                    if (type == "Overload")
                    {
                        id += '*';
                    }

                    // When see and seealso are top level nodes in triple slash comments, do not convert it into xref node
                    if (item.Parent?.Parent != null)
                    {
                        XElement replacement;
                        if(string.IsNullOrEmpty(item.Value))
                        {
                            replacement = XElement.Parse($"<xref href=\"{HttpUtility.UrlEncode(id)}\" data-throw-if-not-resolved=\"false\"></xref>");
                        }
                        else
                        {
                            replacement = XElement.Parse($"<xref href=\"{HttpUtility.UrlEncode(id)}?text={HttpUtility.UrlEncode(item.Value)}\" data-throw-if-not-resolved=\"false\"></xref>");
                        }
                        item.ReplaceWith(replacement);
                    }

                    addReference?.Invoke(id, cref);
                    success = true;
                }

                if (!success)
                {
                    var detailedInfo = new StringBuilder();
                    if (_context != null && _context.Source != null)
                    {
                        if (!string.IsNullOrEmpty(_context.Source.Name))
                        {
                            detailedInfo.Append(" for ");
                            detailedInfo.Append(_context.Source.Name);
                        }
                        if (!string.IsNullOrEmpty(_context.Source.Path))
                        {
                            detailedInfo.Append(" defined in ");
                            detailedInfo.Append(_context.Source.Path);
                            detailedInfo.Append(" Line ");
                            detailedInfo.Append(_context.Source.StartLine);
                        }
                    }

                    Logger.Log(LogLevel.Warning, $"Invalid cref value \"{cref}\" found in triple-slash-comments{detailedInfo}, ignored.");
                }
            }
        }
        catch
        {
        }
    }

    private IEnumerable<string> GetMultipleExampleNodes(XPathNavigator navigator, string selector)
    {
        var iterator = navigator.Select(selector);
        if (iterator == null)
        {
            yield break;
        }
        foreach (XPathNavigator nav in iterator)
        {
            yield return GetXmlValue(nav);
        }
    }

    private IEnumerable<ExceptionInfo> GetMulitpleCrefInfo(XPathNavigator navigator, string selector)
    {
        var iterator = navigator.Clone().Select(selector);
        if (iterator == null)
        {
            yield break;
        }
        foreach (XPathNavigator nav in iterator)
        {
            string description = GetXmlValue(nav);
            string commentId = nav.GetAttribute("cref", string.Empty);
            string refId = nav.GetAttribute("refId", string.Empty);
            if (!string.IsNullOrEmpty(refId))
            {
                yield return new ExceptionInfo
                {
                    Description = description,
                    Type = refId,
                    CommentId = commentId
                };
            }
            else if (!string.IsNullOrEmpty(commentId))
            {
                // Check if exception type is valid and trim prefix
                var match = CommentIdRegex.Match(commentId);
                if (match.Success)
                {
                    var id = match.Groups["id"].Value;
                    var type = match.Groups["type"].Value;
                    if (type == "T")
                    {
                        yield return new ExceptionInfo
                        {
                            Description = description,
                            Type = id,
                            CommentId = commentId
                        };
                    }
                }
            }
        }
    }

    private IEnumerable<LinkInfo> GetMultipleLinkInfo(XPathNavigator navigator, string selector)
    {
        var iterator = navigator.Clone().Select(selector);
        if (iterator == null)
        {
            yield break;
        }
        foreach (XPathNavigator nav in iterator)
        {
            string altText = nav.InnerXml.Trim();
            if (string.IsNullOrEmpty(altText))
            {
                altText = null;
            }

            string commentId = nav.GetAttribute("cref", string.Empty);
            string url = nav.GetAttribute("href", string.Empty);
            string refId = nav.GetAttribute("refId", string.Empty);
            if (!string.IsNullOrEmpty(refId))
            {
                yield return new LinkInfo
                {
                    AltText = altText,
                    LinkId = refId,
                    CommentId = commentId,
                    LinkType = LinkType.CRef
                };
            }
            else if (!string.IsNullOrEmpty(commentId))
            {
                // Check if cref type is valid and trim prefix
                var match = CommentIdRegex.Match(commentId);
                if (match.Success)
                {
                    var id = match.Groups["id"].Value;
                    var type = match.Groups["type"].Value;
                    if (type == "Overload")
                    {
                        id += '*';
                    }

                    yield return new LinkInfo
                    {
                        AltText = altText,
                        LinkId = id,
                        CommentId = commentId,
                        LinkType = LinkType.CRef
                    };
                }
            }
            else if (!string.IsNullOrEmpty(url))
            {
                yield return new LinkInfo
                {
                    AltText = altText ?? url,
                    LinkId = url,
                    LinkType = LinkType.HRef
                };
            }
        }
    }

    private string GetSingleNodeValue(XPathNavigator nav, string selector)
    {
        return GetXmlValue(nav.Clone().SelectSingleNode(selector));
    }

    private string GetXmlValue(XPathNavigator node)
    {
        if (node is null)
            return null;

        return TrimEachLine(GetInnerXml(node));
    }

    private static string TrimEachLine(string text, string indent = "")
    {
        var minLeadingWhitespace = int.MaxValue;
        var lines = ReadLines(text).ToList();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var leadingWhitespace = 0;
            while (leadingWhitespace < line.Length && char.IsWhiteSpace(line[leadingWhitespace]))
                leadingWhitespace++;

            minLeadingWhitespace = Math.Min(minLeadingWhitespace, leadingWhitespace);
        }

        var builder = new StringBuilder();

        // Trim leading empty lines
        var trimStart = true;

        // Apply indentation to all lines except the first,
        // since the first new line in <pre></code> is significant
        var firstLine = true;

        foreach (var line in lines)
        {
            if (trimStart && string.IsNullOrWhiteSpace(line))
                continue;

            if (firstLine)
                firstLine = false;
            else
                builder.Append(indent);

            if (string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine();
                continue;
            }

            trimStart = false;
            builder.AppendLine(line.Substring(minLeadingWhitespace));
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// `>` is always encoded to `&gt;` in XML, when triple-slash-comments is considered as Markdown content, `>` is considered as blockquote
    /// Decode `>` to enable the Markdown syntax considering `>` is not a Must-Encode in Text XElement
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static string GetInnerXml(XPathNavigator node)
    {
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        using (var tw = new XmlWriterWithGtDecoded(sw))
        {
            if (node.MoveToFirstChild())
            {
                do
                {
                    tw.WriteNode(node, true);
                } while (node.MoveToNext());
                node.MoveToParent();
            }
        }

        return sw.ToString();
    }

    private sealed class XmlWriterWithGtDecoded : XmlTextWriter
    {
        public XmlWriterWithGtDecoded(TextWriter tw) : base(tw) { }

        public XmlWriterWithGtDecoded(Stream w, Encoding encoding) : base(w, encoding) { }

        public override void WriteString(string text)
        {
            var encoded = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace("'", "&apos;").Replace("\"", "&quot;");
            WriteRaw(encoded);
        }
    }
}
