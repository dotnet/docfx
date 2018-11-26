// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using System.Globalization;

    public class TripleSlashCommentModel
    {
        private const string idSelector = @"((?![0-9])[\w_])+[\w\(\)\.\{\}\[\]\|\*\^~#@!`,_<>:]*";
        private static Regex CommentIdRegex = new Regex(@"^(?<type>N|T|M|P|F|E|Overload):(?<id>" + idSelector + ")$", RegexOptions.Compiled);
        private static Regex LineBreakRegex = new Regex(@"\r?\n", RegexOptions.Compiled);
        private static Regex CodeElementRegex = new Regex(@"<code[^>]*>([\s\S]*?)</code>", RegexOptions.Compiled);
        private static Regex RegionRegex = new Regex(@"^\s*#region\s*(.*)$");
        private static Regex EndRegionRegex = new Regex(@"^\s*#endregion\s*.*$");

        private readonly ITripleSlashCommentParserContext _context;

        public string Summary { get; private set; }

        public string Remarks { get; private set; }

        public string Returns { get; private set; }

        public List<ExceptionInfo> Exceptions { get; private set; }

        public List<LinkInfo> Sees { get; private set; }

        public List<LinkInfo> SeeAlsos { get; private set; }

        public List<string> Examples { get; private set; }

        public Dictionary<string, string> Parameters { get; private set; }

        public Dictionary<string, string> TypeParameters { get; private set; }

        public bool IsInheritDoc { get; private set; }

        private TripleSlashCommentModel(string xml, SyntaxLanguage language, ITripleSlashCommentParserContext context)
        {
            // Transform triple slash comment
            XDocument doc = TripleSlashCommentTransformer.Transform(xml, language);

            _context = context;
            if (!context.PreserveRawInlineComments)
            {
                ResolveSeeCref(doc, context.AddReferenceDelegate, context.ResolveCRef);
                ResolveSeeAlsoCref(doc, context.AddReferenceDelegate, context.ResolveCRef);
                ResolveExceptionCref(doc, context.AddReferenceDelegate, context.ResolveCRef);
            }
            ResolveCodeSource(doc, context);
            var nav = doc.CreateNavigator();
            Summary = GetSummary(nav, context);
            Remarks = GetRemarks(nav, context);
            Returns = GetReturns(nav, context);

            Exceptions = GetExceptions(nav, context);
            Sees = GetSees(nav, context);
            SeeAlsos = GetSeeAlsos(nav, context);
            Examples = GetExamples(nav, context);
            Parameters = GetParameters(nav, context);
            TypeParameters = GetTypeParameters(nav, context);
            IsInheritDoc = GetIsInheritDoc(nav, context);
        }

        public static TripleSlashCommentModel CreateModel(string xml, SyntaxLanguage language, ITripleSlashCommentParserContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (string.IsNullOrEmpty(xml)) return null;

            // Quick turnaround for badly formed XML comment
            if (xml.StartsWith("<!-- Badly formed XML comment ignored for member "))
            {
                Logger.LogWarning($"Invalid triple slash comment is ignored: {xml}");
                return null;
            }
            try
            {
                var model = new TripleSlashCommentModel(xml, language, context);
                return model;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        public void CopyInheritedData(TripleSlashCommentModel src)
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
            if (Sees == null && src.Sees != null)
            {
                Sees = src.Sees.Select(s => s.Clone()).ToList();
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

        /// <summary>
        /// Get summary node out from triple slash comments
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        /// <example>
        /// <code> <see cref="Hello"/></code>
        /// </example>
        private string GetSummary(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            // Resolve <see cref> to @ syntax
            // Also support <seealso cref>
            string selector = "/member/summary";
            return GetSingleNodeValue(nav, selector);
        }

        /// <summary>
        /// Get remarks node out from triple slash comments
        /// </summary>
        /// <remarks>
        /// <para>This is a sample of exception node</para>
        /// </remarks>
        /// <param name="xml"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        private string GetRemarks(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            string selector = "/member/remarks";
            return GetSingleNodeValue(nav, selector);
        }

        private string GetReturns(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            // Resolve <see cref> to @ syntax
            // Also support <seealso cref>
            string selector = "/member/returns";
            return GetSingleNodeValue(nav, selector);
        }

        /// <summary>
        /// Get exceptions nodes out from triple slash comments
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        /// <exception cref="XmlException">This is a sample of exception node</exception>
        private List<ExceptionInfo> GetExceptions(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            string selector = "/member/exception";
            var result = GetMulitpleCrefInfo(nav, selector).ToList();
            if (result.Count == 0)
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// To get `see` tags out
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <see cref="SpecIdHelper"/>
        /// <see cref="SourceSwitch"/>
        private List<LinkInfo> GetSees(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            var result = GetMultipleLinkInfo(nav, "/member/see").ToList();
            if (result.Count == 0)
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// To get `seealso` tags out
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <seealso cref="WaitForChangedResult"/>
        /// <seealso cref="http://google.com">ABCS</seealso>
        private List<LinkInfo> GetSeeAlsos(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            var result = GetMultipleLinkInfo(nav, "/member/seealso").ToList();
            if (result.Count == 0)
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// To get `example` tags out
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to call the <see cref="GetExceptions(string, ITripleSlashCommentParserContext)"/> method.
        /// <code>
        /// class TestClass
        /// {
        ///     static int Main()
        ///     {
        ///         return GetExceptions(null, null).Count();
        ///     }
        /// }
        /// </code>
        /// </example>
        private List<string> GetExamples(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            // Resolve <see cref> to @ syntax
            // Also support <seealso cref>
            return GetMultipleExampleNodes(nav, "/member/example").ToList();
        }

        private bool GetIsInheritDoc(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            var node = nav.SelectSingleNode("/member/inheritdoc");
            if (node == null)
            {
                return false;
            }
            if (node.HasAttributes)
            {
                //The Sandcastle implementation of <inheritdoc /> supports two attributes: 'cref' and 'select'.
                //These attributes allow changing the source of the inherited doc and controlling what is inherited.
                //Until these attributes are supported, ignoring inheritdoc elements with attributes, so as not to misinterpret them.
                Logger.LogWarning("Attributes on <inheritdoc /> elements are not supported; inheritdoc element will be ignored.");
                return false;
            }
            return true;
        }

        private void ResolveCodeSource(XDocument doc, ITripleSlashCommentParserContext context)
        {
            foreach (XElement node in doc.XPathSelectElements("//code"))
            {
                var source = node.Attribute("source");
                if (source == null || string.IsNullOrEmpty(source.Value))
                {
                    continue;
                }

                if (context.Source == null || string.IsNullOrEmpty(context.Source.Path))
                {
                    Logger.LogWarning($"Unable to get source file path for {node.ToString()}");
                    return;
                }

                var region = node.Attribute("region");

                var path = source.Value;
                if (!Path.IsPathRooted(path))
                {
                    var basePath = !string.IsNullOrEmpty(context.CodeSourceBasePath) ? context.CodeSourceBasePath : Path.GetDirectoryName(Path.Combine(EnvironmentContext.BaseDirectory, context.Source.Path));
                    
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

            var builder = new StringBuilder();
            var regionCount = 0;
            foreach (var line in File.ReadLines(source))
            {
                if (!string.IsNullOrEmpty(region))
                {
                    var match = RegionRegex.Match(line);
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
                    else if (regionCount > 0 && EndRegionRegex.IsMatch(line))
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

            element.SetValue(builder.ToString());
        }

        private Dictionary<string, string> GetListContent(XPathNavigator navigator, string xpath, string contentType, ITripleSlashCommentParserContext context)
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
                        string path = context.Source.Remote != null ? Path.Combine(EnvironmentContext.BaseDirectory, context.Source.Remote.RelativePath) : context.Source.Path;
                        Logger.LogWarning($"Duplicate {contentType} '{name}' found in comments, the latter one is ignored.", file: StringExtension.ToDisplayPath(path), line: context.Source.StartLine.ToString());
                    }
                    else
                    {
                        result.Add(name, description);
                    }
                }
            }

            return result;
        }

        private Dictionary<string, string> GetParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            return GetListContent(navigator, "/member/param", "parameter", context);
        }

        private Dictionary<string, string> GetTypeParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            return GetListContent(navigator, "/member/typeparam", "type parameter", context);
        }

        private void ResolveSeeAlsoCref(XNode node, Action<string, string> addReference, Func<string, CRefTarget> resolveCRef)
        {
            // Resolve <see cref> to <xref>
            ResolveCrefLink(node, "//seealso[@cref]", addReference, resolveCRef);
        }

        private void ResolveSeeCref(XNode node, Action<string, string> addReference, Func<string, CRefTarget> resolveCRef)
        {
            // Resolve <see cref> to <xref>
            ResolveCrefLink(node, "//see[@cref]", addReference, resolveCRef);
        }

        private void ResolveExceptionCref(XNode node, Action<string, string> addReference, Func<string, CRefTarget> resolveCRef)
        {
            ResolveCrefLink(node, "//exception[@cref]", addReference, resolveCRef);
        }

        private void ResolveCrefLink(XNode node, string nodeSelector, Action<string, string> addReference, Func<string, CRefTarget> resolveCRef)
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

                    if (resolveCRef != null)
                    {
                        // The resolveCRef delegate resolves the cref and returns the name of a reference if successful.
                        var cRefTarget = resolveCRef.Invoke(cref);
                        if (cRefTarget != null)
                        {
                            if (item.Parent?.Parent == null)
                            {   
                                // <see> or <seealso> is top-level tag. Keep it, but set resolved references.
                                item.SetAttributeValue("refId", cRefTarget.Id);
                                item.SetAttributeValue("cref", cRefTarget.CommentId);
                            }
                            else
                            {
                                // <see> occurs in text. Replace it with an <xref> node using the resolved reference.
                                var replacement = XElement.Parse($"<xref href=\"{HttpUtility.UrlEncode(cRefTarget.Id)}\" data-throw-if-not-resolved=\"false\"></xref>");
                                item.ReplaceWith(replacement);
                            }
                            success = true;
                        }
                        else
                        {
                            item.Remove();
                            success = false;
                        }
                    }
                    else
                    {
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
                                var replacement = XElement.Parse($"<xref href=\"{HttpUtility.UrlEncode(id)}\" data-throw-if-not-resolved=\"false\"></xref>");
                                item.ReplaceWith(replacement);
                            }

                            addReference?.Invoke(id, cref);
                            success = true;
                        }
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
                string altText = GetXmlValue(nav);
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
            // NOTE: use node.InnerXml instead of node.Value, to keep decorative nodes,
            // e.g.
            // <remarks><para>Value</para></remarks>
            // decode InnerXml as it encodes
            // IXmlLineInfo.LinePosition starts from 1 and it would ignore '<'
            // e.g.
            // <summary/> the LinePosition is the column number of 's', so it should be minus 2
            var lineInfo = node as IXmlLineInfo;
            int column = lineInfo.HasLineInfo() ? lineInfo.LinePosition - 2 : 0;

            return NormalizeXml(RemoveLeadingSpaces(GetInnerXml(node)), column);
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

        /// <summary>
        /// Split xml into lines. Trim meaningless whitespaces.
        /// if a line starts with xml node, all leading whitespaces would be trimmed
        /// otherwise text node start position always aligns with the start position of its parent line(the last previous line that starts with xml node)
        /// Trim newline character for code element.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="parentIndex">the start position of the last previous line that starts with xml node</param>
        /// <returns>normalized xml</returns>
        private static string NormalizeXml(string xml, int parentIndex)
        {
            var lines = LineBreakRegex.Split(xml);
            var normalized = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    normalized.Add(string.Empty);
                }
                else
                {
                    // TO-DO: special logic for TAB case
                    int index = line.TakeWhile(char.IsWhiteSpace).Count();
                    if (line[index] == '<')
                    {
                        parentIndex = index;
                    }

                    normalized.Add(line.Substring(Math.Min(parentIndex, index)));
                }
            }

            // trim newline character for code element
            return CodeElementRegex.Replace(
                string.Join("\n", normalized),
                m =>
                {
                    var group = m.Groups[1];
                    if (group.Length == 0)
                    {
                        return m.Value;
                    }
                    return m.Value.Replace(group.ToString(), group.ToString().Trim('\n'));
                });
        }

        /// <summary>
        /// `>` is always encoded to `&gt;` in XML, when triple-slash-comments is considered as Markdown content, `>` is considered as blockquote
        /// Decode `>` to enable the Markdown syntax considering `>` is not a Must-Encode in Text XElement
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string GetInnerXml(XPathNavigator node)
        {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture))
            {
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
}
