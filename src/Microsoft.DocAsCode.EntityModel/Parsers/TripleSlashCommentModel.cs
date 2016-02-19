// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TripleSlashCommentModel
    {
        private const string idSelector = @"((?![0-9])[\w_])+[\w\(\)\.\{\}\[\]\|\*\^~#@!`,_<>:]*";
        private static Regex CommentIdRegex = new Regex(@"^(?<type>N|T|M|P|F|E):(?<id>" + idSelector + ")$", RegexOptions.Compiled);

        public string Summary { get; private set; }
        public string Remarks { get; private set; }
        public string Returns { get; private set; }
        public List<CrefInfo> Exceptions { get; private set; }
        public List<CrefInfo> Sees { get; private set; }
        public List<CrefInfo> SeeAlsos { get; private set; }
        public List<string> Examples { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }
        public Dictionary<string, string> TypeParameters { get; private set; }

        private TripleSlashCommentModel() { }

        public static TripleSlashCommentModel CreateModel(string xml, ITripleSlashCommentParserContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(xml)) return null;
            // Quick turnaround for badly formed XML comment
            if (xml.StartsWith("<!-- Badly formed XML comment ignored for member "))
            {
                Logger.LogWarning($"Invalid triple slash comment is ignored: {xml}");
                return null;
            }
            try
            {
                // Normalize xml line ending before load into xml
                XDocument doc = XDocument.Parse(xml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
                if (!context.PreserveRawInlineComments)
                {
                    ResolveSeeCref(doc, context.AddReferenceDelegate);
                    ResolveSeeAlsoCref(doc, context.AddReferenceDelegate);
                    ResolveParameterRef(doc);
                }
                var nav = doc.CreateNavigator();

                var model = new TripleSlashCommentModel();
                model.Summary = GetSummary(nav, context);
                model.Remarks = GetRemarks(nav, context);
                model.Returns = GetReturns(nav, context);

                model.Exceptions = GetExceptions(nav, context);
                model.Sees = GetSees(nav, context);
                model.SeeAlsos = GetSeeAlsos(nav, context);
                model.Examples = GetExamples(nav, context);
                model.Parameters = GetParameters(nav, context);
                model.TypeParameters = GetTypeParameters(nav, context);
                return model;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        public string GetParameter(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return GetValue(name, Parameters);
        }

        public string GetTypeParameter(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return GetValue(name, TypeParameters);
        }

        private static string GetValue(string name, Dictionary<string, string> dictionary)
        {
            if (dictionary == null) return null;
            string description;
            if (dictionary.TryGetValue(name, out description))
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
        private static string GetSummary(XPathNavigator nav, ITripleSlashCommentParserContext context)
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
        private static string GetRemarks(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            string selector = "/member/remarks";
            return GetSingleNodeValue(nav, selector);
        }

        private static string GetReturns(XPathNavigator nav, ITripleSlashCommentParserContext context)
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
        private static List<CrefInfo> GetExceptions(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            string selector = "/member/exception";
            var result = GetMulitpleCrefInfo(nav, selector).ToList();
            if (result.Count == 0) return null;
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
        private static List<CrefInfo> GetSees(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            var result = GetMulitpleCrefInfo(nav, "/member/see").ToList();
            if (result.Count == 0) return null;
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
        private static List<CrefInfo> GetSeeAlsos(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            var result = GetMulitpleCrefInfo(nav, "/member/seealso").ToList();
            if (result.Count == 0) return null;
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
        private static List<string> GetExamples(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            // Resolve <see cref> to @ syntax
            // Also support <seealso cref>
            return GetMultipleExampleNodes(nav, "/member/example").ToList();
        }

        private static Dictionary<string, string> GetListContent(XPathNavigator navigator, string xpath, string contentType, ITripleSlashCommentParserContext context)
        {
            var iterator = navigator.Select(xpath);
            var result = new Dictionary<string, string>();
            if (iterator == null) return result;
            foreach (XPathNavigator nav in iterator)
            {
                string name = nav.GetAttribute("name", string.Empty);
                string description = GetXmlValue(nav);
                if (!string.IsNullOrEmpty(name))
                {
                    if (result.ContainsKey(name))
                    {
                        string path = context.Source.Remote != null ? Path.Combine(context.Source.Remote.LocalWorkingDirectory, context.Source.Remote.RelativePath) : context.Source.Path;
                        Logger.LogWarning($"Duplicate {contentType} '{name}' found in comments, the latter one is ignored.", null, path.ToDisplayPath(), context.Source.StartLine.ToString());
                    }
                    else
                    {
                        result.Add(name, description);
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, string> GetParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            return GetListContent(navigator, "/member/param", "parameter", context);
        }

        private static Dictionary<string, string> GetTypeParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            return GetListContent(navigator, "/member/typeparam", "type parameter", context);
        }

        /// <summary>
        /// <paramref name="Hello"/>
        /// </summary>
        /// <param name="node"></param>
        private static void ResolveParameterRef(XDocument node)
        {
            var paramRefs = node.Descendants("paramref").ToList();
            foreach (var paramRef in paramRefs)
            {
                var name = paramRef.Attribute("name");
                if (name != null)
                {
                    // Convert paramref to italic
                    paramRef.ReplaceWith("*" + name.Value + "*");
                }
                else
                {
                    paramRef.Remove();
                }
            }
        }

        private static void ResolveSeeAlsoCref(XNode node, Action<string> addReference)
        {
            // Resolve <see cref> to <xref>
            ResolveCrefLink(node, "//seealso", addReference);
        }

        private static void ResolveSeeCref(XNode node, Action<string> addReference)
        {
            // Resolve <see cref> to <xref>
            ResolveCrefLink(node, "//see", addReference);
        }

        private static void ResolveCrefLink(XNode node, string nodeSelector, Action<string> addReference)
        {
            if (node == null || string.IsNullOrEmpty(nodeSelector)) return;

            try
            {
                
                var nodes = node.XPathSelectElements(nodeSelector + "[@cref]").ToList();
                foreach(var item in nodes)
                {
                    var value = item.Attribute("cref").Value;
                    // Strict check is needed as value could be an invalid href, 
                    // e.g. !:Dictionary&lt;TKey, string&gt; when user manually changed the intellisensed generic type
                    if (CommentIdRegex.IsMatch(value))
                    {
                        value = value.Substring(2);
                        var replacement = XElement.Parse($"<xref href=\"{WebUtility.HtmlEncode(value)}\" data-throw-if-not-resolved=\"false\"></xref>");
                        item.ReplaceWith(replacement);
                        if (addReference != null)
                        {
                            addReference(value);
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, $"Invalid cref value {value} found in triple-slash-comments, ignored.");
                    }
                }
            }
            catch
            {
            }
        }

        private static IEnumerable<string> GetMultipleExampleNodes(XPathNavigator navigator, string selector)
        {
            var iterator = navigator.Select(selector);
            if (iterator == null) yield break;
            foreach (XPathNavigator nav in iterator)
            {
                string description = GetXmlValue(nav);
                yield return description;
            }
        }

        private static IEnumerable<CrefInfo> GetMulitpleCrefInfo(XPathNavigator navigator, string selector)
        {
            var iterator = navigator.Clone().Select(selector);
            if (iterator == null) yield break;
            foreach (XPathNavigator nav in iterator)
            {
                string description = GetXmlValue(nav);

                string type = nav.GetAttribute("cref", string.Empty);
                if (!string.IsNullOrEmpty(type))
                {
                    // Check if exception type is valid and trim prefix
                    if (CommentIdRegex.IsMatch(type))
                    {
                        type = type.Substring(2);
                        if (string.IsNullOrEmpty(description)) description = null;
                        yield return new CrefInfo { Description = description, Type = type };
                    }
                }
            }
        }

        private static string GetSingleNodeValue(XPathNavigator nav, string selector)
        {
            var node = nav.Clone().SelectSingleNode(selector);
            if (node == null)
            {
                // throw new ArgumentException(selector + " is not found");
                return null;
            }
            else
            {
                var output = GetXmlValue(node);
                return output;
            }
        }
        
        /// <summary>
        /// For multiple line comments, comment start position always aligns with its node tag's start position
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string GetXmlValue(XPathNavigator node)
        {
            // NOTE: use node.InnerXml instead of node.Value, to keep decorative nodes,
            // e.g.
            // <remarks><para>Value</para></remarks>
            // decode InnerXml as it encodes
            var lineInfo = node as IXmlLineInfo;
            var column = lineInfo.LinePosition;
            var content = WebUtility.HtmlDecode(node.InnerXml);
            var allLines = GetLines(content);

            // Save Crlf/Lf to LineInfo so that the original line ending is saved
            var lines = allLines.Select(s => new { content = NormalizeLine(s.Content, column, s.StartIndex), prefix = s.Prefix }).SelectMany(s => new string[] { s.prefix, s.content });

            return string.Join(string.Empty, lines);
        }

        private const string Crlf = "\r\n";
        private const string Lf = "\n";

        /// <summary>
        /// The line position from XElement contains line ending from previous line
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static IEnumerable<LineInfo> GetLines(string content)
        {
            var lines = content.Split(new string[] { Crlf }, StringSplitOptions.None);
            var firstLine = lines[0];
            var firstLineInfo = new LineInfo(lines[0], 0, string.Empty);
            foreach (var line in GetLfLinesFromLine(firstLineInfo))
            {
                yield return line;
            }
            for (var i = 1; i < lines.Length; i++)
            {
                var crlfLine = new LineInfo(lines[i], Crlf.Length, Crlf);
                foreach (var line in GetLfLinesFromLine(crlfLine))
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<LineInfo> GetLfLinesFromLine(LineInfo info)
        {
            var content = info.Content;
            var lines = content.Split(Lf[0]);
            yield return new LineInfo(lines[0], info.StartIndex, info.Prefix);
            for (var i = 1; i < lines.Length; i++)
            {
                yield return new LineInfo(lines[i], Lf.Length, Lf);
            }
        }

        private static string NormalizeLine(string line, int column, int startIndex)
        {
            int trimIndex = 0;
            column = column - startIndex;
            while (trimIndex < column && trimIndex < line.Length && char.IsWhiteSpace(line[trimIndex]))
            {
                trimIndex++;
            }

            return line.Substring(trimIndex);
        }

        private sealed class LineInfo
        {
            public string Content { get; }
            public int StartIndex { get; }

            public string Prefix { get; }
            public LineInfo(string content, int startIndex, string prefix)
            {
                Content = content;
                StartIndex = startIndex;
                Prefix = prefix;
            }
        }
    }
}
