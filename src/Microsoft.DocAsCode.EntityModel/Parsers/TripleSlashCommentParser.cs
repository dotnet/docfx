// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.XPath;

    public interface ITripleSlashCommentParserContext
    {
        bool Normalize { get; set; }
        bool PreserveRawInlineComments { get; set; }
        Action<string> AddReferenceDelegate { get; set; }
    }

    public class TripleSlashCommentParserContext : ITripleSlashCommentParserContext
    {
        public bool Normalize { get; set; } = true;

        public bool PreserveRawInlineComments { get; set; }

        public Action<string> AddReferenceDelegate { get; set; }

    }

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
            if (xml.StartsWith("<!-- Badly formed XML comment ignored for member ")) return null;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                var nav = doc.CreateNavigator();
                if (!context.PreserveRawInlineComments)
                {
                    ResolveSeeCref(nav, string.Empty, context.AddReferenceDelegate);
                    ResolveSeeAlsoCref(nav, string.Empty, context.AddReferenceDelegate);
                    ResolveParameterRef(nav);
                }

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
            return GetSingleNodeValue(nav, selector, context.Normalize);
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
            return GetSingleNodeValue(nav, selector, context.Normalize);
        }

        private static string GetReturns(XPathNavigator nav, ITripleSlashCommentParserContext context)
        {
            // Resolve <see cref> to @ syntax
            // Also support <seealso cref>
            string selector = "/member/returns";
            return GetSingleNodeValue(nav, selector, context.Normalize);
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
            var result = GetMulitpleCrefInfo(nav, selector, context.Normalize).ToList();
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
            var result = GetMulitpleCrefInfo(nav, "/member/see", context.Normalize).ToList();
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
            var result = GetMulitpleCrefInfo(nav, "/member/seealso", context.Normalize).ToList();
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
            return GetMultipleExampleNodes(nav, "/member/example", context.Normalize).ToList();
        }

        private static Dictionary<string, string> GetParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            var iterator = navigator.Select("/member/param");
            var result = new Dictionary<string, string>();
            if (iterator == null) return result;
            foreach (XPathNavigator nav in iterator)
            {
                string name = nav.GetAttribute("name", string.Empty);
                string description = nav.Value;
                if (context.Normalize) description = NormalizeContentFromTripleSlashComment(description);
                if (!string.IsNullOrEmpty(name)) result.Add(name, description);
            }

            return result;
        }

        public static Dictionary<string, string> GetTypeParameters(XPathNavigator navigator, ITripleSlashCommentParserContext context)
        {
            var iterator = navigator.Select("/member/typeparam");
            var result = new Dictionary<string, string>();
            if (iterator == null) return result;
            foreach (XPathNavigator nav in iterator)
            {
                string name = nav.GetAttribute("name", string.Empty);
                string description = nav.Value;
                if (context.Normalize) description = NormalizeContentFromTripleSlashComment(description);
                if (!string.IsNullOrEmpty(name)) result.Add(name, description);
            }

            return result;
        }

        private static void ResolveParameterRef(XPathNavigator nav)
        {
            var paramRefs = nav.Select("//paramref").OfType<XPathNavigator>().ToArray();
            foreach (var paramRef in paramRefs)
            {
                var name = paramRef.SelectSingleNode("@name");
                if (name != null)
                {
                    // Convert paramref to italic
                    paramRef.InsertAfter("*" + name.Value + "*");
                }
            }

            foreach (var paramRef in paramRefs)
            {
                paramRef.DeleteSelf();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="nodeSelector"></param>
        /// <returns></returns>
        private static void ResolveSeeAlsoCref(XPathNavigator nav, string nodeSelector, Action<string> addReference)
        {
            // Resolve <see cref> to @ syntax
            ResolveCrefLink(nav, nodeSelector + "//seealso", addReference);
        }

        private static void ResolveSeeCref(XPathNavigator nav, string nodeSelector, Action<string> addReference)
        {
            // Resolve <see cref> to @ syntax
            ResolveCrefLink(nav, nodeSelector + "//see", addReference);
        }

        private static void ResolveCrefLink(XPathNavigator nav, string nodeSelector, Action<string> addReference)
        {
            if (nav == null || string.IsNullOrEmpty(nodeSelector)) return;

            try
            {
                var iter = nav.Select(nodeSelector + "[@cref]");
                List<XPathNavigator> sees = new List<XPathNavigator>();
                foreach (XPathNavigator i in iter)
                {
                    var node = i.SelectSingleNode("@cref");
                    if (node != null)
                    {
                        var value = node.Value;

                        // Strict check is needed as value could be an invalid href, 
                        // e.g. !:Dictionary&lt;TKey, string&gt; when user manually changed the intellisensed generic type
                        if (CommentIdRegex.IsMatch(value))
                        {
                            value = value.Substring(2);
                            i.InsertAfter("@'" + value + "'");

                            sees.Add(i);
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

                // on successful deleteself, i would point to its parent
                foreach (XPathNavigator i in sees)
                {
                    i.DeleteSelf();
                }
            }
            catch
            {
            }
        }

        private static IEnumerable<string> GetMultipleExampleNodes(XPathNavigator navigator, string selector, bool normalize)
        {
            var iterator = navigator.Select(selector);
            if (iterator == null) yield break;
            foreach (XPathNavigator nav in iterator)
            {
                // NOTE: use node.InnerXml instead of node.Value, to keep decorative nodes
                string description = nav.InnerXml;
                if (normalize) description = NormalizeContentFromTripleSlashComment(description);
                yield return description;
            }
        }

        private static IEnumerable<CrefInfo> GetMulitpleCrefInfo(XPathNavigator navigator, string selector, bool normalize)
        {
            var iterator = navigator.Clone().Select(selector);
            if (iterator == null) yield break;
            foreach (XPathNavigator nav in iterator)
            {
                string description = nav.Value;
                if (normalize) description = NormalizeContentFromTripleSlashComment(description);

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

        private static string GetSingleNodeValue(XPathNavigator nav, string selector, bool normalize)
        {
            var node = nav.Clone().SelectSingleNode(selector);
            if (node == null)
            {
                // throw new ArgumentException(selector + " is not found");
                return null;
            }
            else
            {
                // NOTE: use node.InnerXml instead of node.Value, to keep decorative nodes,
                // e.g.
                // <remarks><para>Value</para></remarks>
                var output = node.InnerXml;
                if (normalize) output = NormalizeContentFromTripleSlashComment(output);
                return output;
            }
        }

        /// <summary>
        /// The issue with GetXmlDocumentationXML is that it append /r/n and 4 spaces to the new line,
        /// which is considered as code in Markdown syntax
        /// </summary>
        /// <param name="content">The content from triple slash comment</param>
        /// <returns></returns>
        private static string NormalizeContentFromTripleSlashComment(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            StringBuilder builder = new StringBuilder();
            using (StringReader reader = new StringReader(content))
            {
                string line;
                // Trim spaces for each line, thus actually Tab to indent is not supported...while new line is kept
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    builder.AppendLine(line);
                }
            }

            // Trim again, e.g. <summary> always starts a new line and thus a \r\n is the first line
            return builder.ToString().Trim();
        }
    }
}
