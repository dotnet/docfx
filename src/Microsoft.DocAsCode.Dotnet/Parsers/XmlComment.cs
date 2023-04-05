// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Markdig;
using Markdig.Renderers.Roundtrip;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal class XmlComment
{
    private const string idSelector = @"((?![0-9])[\w_])+[\w\(\)\.\{\}\[\]\|\*\^~#@!`,_<>:]*";
    private static readonly Regex CommentIdRegex = new(@"^(?<type>N|T|M|P|F|E|Overload):(?<id>" + idSelector + ")$", RegexOptions.Compiled);
    private static readonly Regex LineBreakRegex = new(@"\r?\n", RegexOptions.Compiled);
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
        ResolveSeeCref(doc, context.AddReferenceDelegate);
        ResolveSeeAlsoCref(doc, context.AddReferenceDelegate);
        ResolveExceptionCref(doc, context.AddReferenceDelegate);

        ResolveCodeSource(doc, context);
        var nav = doc.CreateNavigator();
        Summary = GetSummary(nav, context);
        Remarks = GetRemarks(nav, context);
        Returns = GetReturns(nav, context);

        Exceptions = GetExceptions(nav, context);
        SeeAlsos = GetSeeAlsos(nav, context);
        Examples = GetExamples(nav, context);
        Parameters = GetParameters(nav, context);
        TypeParameters = GetTypeParameters(nav, context);
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

    public void CopyInheritedData(XmlComment src)
    {
        if (src == null)
        {
            throw new ArgumentNullException(nameof(src));
        }

        Summary = Summary ?? src.Summary;
        Remarks = Remarks ?? src.Remarks;
        Returns = Returns ?? src.Returns;
        if (Exceptions == null && src.Exceptions != null)
        {
            Exceptions = src.Exceptions.Select(e => e.Clone()).ToList();
        }
        if (SeeAlsos == null && src.SeeAlsos != null)
        {
            SeeAlsos = src.SeeAlsos.Select(s => s.Clone()).ToList();
        }
        if (Examples == null && src.Examples != null)
        {
            Examples = new List<string>(src.Examples);
        }
        if (Parameters == null && src.Parameters != null)
        {
            Parameters = new Dictionary<string, string>(src.Parameters);
        }
        if (TypeParameters == null && src.TypeParameters != null)
        {
            TypeParameters = new Dictionary<string, string>(src.TypeParameters);
        }
    }

    public string GetParameter(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        return GetValue(name, Parameters);
    }

    public string GetTypeParameter(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        return GetValue(name, TypeParameters);
    }

    private static string GetValue(string name, Dictionary<string, string> dictionary)
    {
        if (dictionary == null)
        {
            return null;
        }
        if (dictionary.TryGetValue(name, out string description))
        {
            return description;
        }
        return null;
    }

    private string GetSummary(XPathNavigator nav, XmlCommentParserContext context)
    {
        // Resolve <see cref> to @ syntax
        // Also support <seealso cref>
        string selector = "/member/summary";
        return GetSingleNodeValue(nav, selector);
    }

    private string GetRemarks(XPathNavigator nav, XmlCommentParserContext context)
    {
        string selector = "/member/remarks";
        return GetSingleNodeValue(nav, selector);
    }

    private string GetReturns(XPathNavigator nav, XmlCommentParserContext context)
    {
        // Resolve <see cref> to @ syntax
        // Also support <seealso cref>
        string selector = "/member/returns";
        return GetSingleNodeValue(nav, selector);
    }

    private List<ExceptionInfo> GetExceptions(XPathNavigator nav, XmlCommentParserContext context)
    {
        string selector = "/member/exception";
        var result = GetMulitpleCrefInfo(nav, selector).ToList();
        if (result.Count == 0)
        {
            return null;
        }
        return result;
    }

    private List<LinkInfo> GetSeeAlsos(XPathNavigator nav, XmlCommentParserContext context)
    {
        var result = GetMultipleLinkInfo(nav, "/member/seealso").ToList();
        if (result.Count == 0)
        {
            return null;
        }
        return result;
    }

    private List<string> GetExamples(XPathNavigator nav, XmlCommentParserContext context)
    {
        // Resolve <see cref> to @ syntax
        // Also support <seealso cref>
        return GetMultipleExampleNodes(nav, "/member/example").ToList();
    }

    private void ResolveCodeSource(XDocument doc, XmlCommentParserContext context)
    {
        foreach (XElement node in doc.XPathSelectElements("//code"))
        {
            var source = node.Attribute("source");
            if (source == null || string.IsNullOrEmpty(source.Value))
            {
                continue;
            }

            var region = node.Attribute("region");

            var path = source.Value;
            if (!Path.IsPathRooted(path))
            {
                string basePath;

                if (!string.IsNullOrEmpty(context.CodeSourceBasePath))
                {
                    basePath = context.CodeSourceBasePath;
                }
                else
                {
                    if (context.Source == null || string.IsNullOrEmpty(context.Source.Path))
                    {
                        Logger.LogWarning($"Unable to get source file path for {node.ToString()}");
                        continue;
                    }

                    basePath = Path.GetDirectoryName(Path.Combine(EnvironmentContext.BaseDirectory, context.Source.Path));
                }
                
                path = Path.Combine(basePath, path);
            }

            ResolveCodeSource(node, path, region?.Value);
        }
    }

    private void ResolveCodeSource(XElement element, string source, string region)
    {
        if (!File.Exists(source))
        {
            Logger.LogWarning($"Source file '{source}' not found.");
            return;
        }

        var (regionRegex, endRegionRegex) = GetRegionRegex(source);

        var builder = new StringBuilder();
        var regionCount = 0;
        foreach (var line in File.ReadLines(source))
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

        element.SetValue(RemoveLeadingSpaces(builder.ToString()));
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

    private Dictionary<string, string> GetParameters(XPathNavigator navigator, XmlCommentParserContext context)
    {
        return GetListContent(navigator, "/member/param", "parameter", context);
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

    private Dictionary<string, string> GetTypeParameters(XPathNavigator navigator, XmlCommentParserContext context)
    {
        return GetListContent(navigator, "/member/typeparam", "type parameter", context);
    }

    private void ResolveSeeAlsoCref(XNode node, Action<string, string> addReference)
    {
        // Resolve <see cref> to <xref>
        ResolveCrefLink(node, "//seealso[@cref]", addReference);
    }

    private void ResolveSeeCref(XNode node, Action<string, string> addReference)
    {
        // Resolve <see cref> to <xref>
        ResolveCrefLink(node, "//see[@cref]", addReference);
    }

    private void ResolveExceptionCref(XNode node, Action<string, string> addReference)
    {
        ResolveCrefLink(node, "//exception[@cref]", addReference);
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
                item.ReplaceWith(new XElement("c", langword));
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
            string description = GetXmlValue(nav);
            yield return description;
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
            if (string.IsNullOrEmpty(description))
            {
                description = null;
            }

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
        var node = nav.Clone().SelectSingleNode(selector);
        if (node == null)
        {
            // throw new ArgumentException(selector + " is not found");
            return null;
        }
        else
        {
            return GetXmlValue(node);
        }
    }

    private string GetXmlValue(XPathNavigator node)
    {
        if (_context.SkipMarkup)
            return node.InnerXml;

        return GetInnerXmlAsMarkdown(RemoveLeadingSpaces(node.InnerXml));
    }

    /// <summary>
    /// Remove least common whitespces in each line of xml
    /// </summary>
    /// <param name="xml"></param>
    /// <returns>xml after removing least common whitespaces</returns>
    private static string RemoveLeadingSpaces(string xml)
    {
        var lines = LineBreakRegex.Split(xml);
        var normalized = new List<string>();

        var preIndex = 0;
        var leadingSpaces = from line in lines
                            where !string.IsNullOrWhiteSpace(line)
                            select line.TakeWhile(char.IsWhiteSpace).Count();

        if (leadingSpaces.Any())
        {
            preIndex = leadingSpaces.Min();
        }

        if (preIndex == 0)
        {
            return xml;
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                normalized.Add(string.Empty);
            }
            else
            {
                normalized.Add(line.Substring(preIndex));
            }
        }
        return string.Join("\n", normalized);
    }

    private static string GetInnerXmlAsMarkdown(string xml)
    {
        if (!xml.Contains('&'))
            return xml;

        xml = HandleBlockQuote(xml);
        var markdown = Markdown.Parse(xml, trackTrivia: true);
        DecodeMarkdownCode(markdown);
        var sw = new StringWriter();
        var rr = new RoundtripRenderer(sw);
        rr.Write(markdown);
        return sw.ToString();

        static string HandleBlockQuote(string xml)
        {
            // > is encoded to &gt; in XML. When interpreted as markdown, > is as blockquote
            // Decode standalone &gt; to > to enable the block quote markdown syntax
            return Regex.Replace(xml, @"^(\s*)&gt;", "$1>", RegexOptions.Multiline);
        }

        static void DecodeMarkdownCode(MarkdownObject node)
        {
            // Commonmark: Entity and numeric character references are treated as literal text in code spans and code blocks
            switch (node)
            {
                case CodeInline codeInline:
                    codeInline.Content = XmlDecode(codeInline.Content);
                    break;

                case CodeBlock codeBlock:
                    codeBlock.Lines = new(XmlDecode(codeBlock.Lines.ToString()));
                    break;

                case ContainerBlock containerBlock:
                    foreach (var child in containerBlock)
                        DecodeMarkdownCode(child);
                    break;

                case ContainerInline containerInline:
                    foreach (var child in containerInline)
                        DecodeMarkdownCode(child);
                    break;

                case LeafBlock leafBlock when leafBlock.Inline is not null:
                    foreach (var child in leafBlock.Inline)
                        DecodeMarkdownCode(child);
                    break;
            }
        }

        static string XmlDecode(string xml)
        {
            return xml
                .Replace("&gt;", ">")
                .Replace("&lt;", "<")
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
        }
    }
}
