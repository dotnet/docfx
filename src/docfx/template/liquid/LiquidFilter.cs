// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class LiquidFilter
    {
        /// <summary>
        /// Custom filter element for Liquid template, works like
        /// {{ lang | name_value_map: '{"vb":"VB","csharp":"C#","cpp":"C++"}' }}
        /// Replaces the csharp on the inside with C#
        /// </summary>
        public static string NameValueMap(string input, string map)
        {
            map = "{" + map + "}";
            var obj = JObject.Parse(map);

            foreach (var (key, value) in obj)
            {
                if (input.Contains(key))
                {
                    input = input.Replace(key, value?.ToString());
                }
            }

            return input;
        }

        /// <summary>
        /// Filter to append query string to specific html nodes
        /// Liquid syntax example:
        /// {%- if {{context.moniker}} != null -%}
        ///   {%- assign body_content = {{content}} | append_query_string: ""//*[@data-linktype='relativepath']"", ""@href|@src"", ""view"", {{context.moniker}} -%}
        ///   {{ body_content }}
        /// {%- endif -%}
        /// </summary>
        /// <param name="html">Html content to be filtered</param>
        /// <param name="nodesXPath">Nodes' XPath to be selected</param>
        /// <param name="attributesXPath">Attributes' XPath to append query string</param>
        /// <param name="queryKey">The appended query key</param>
        /// <param name="queryValue">The appended query value</param>
        /// <returns>String result</returns>
        public static string AppendQueryString(string html, string nodesXPath, string attributesXPath, string queryKey, string queryValue)
        {
            var htmlDocument = new HtmlDocument();
            try
            {
                htmlDocument.LoadHtml(html);
            }
            catch (Exception)
            {
                return html;
            }

            var documentNode = htmlDocument.DocumentNode;
            var nodes = documentNode.SelectNodes(nodesXPath);
            if (nodes is null || nodes.Count == 0)
            {
                return html;
            }

            foreach (var node in nodes)
            {
                var attributes = node.CreateNavigator().Select(attributesXPath);
                if (attributes.Count == 0)
                {
                    continue;
                }
                foreach (var attribute in attributes)
                {
                    if (attribute is HtmlNodeNavigator nagivator)
                    {
                        var original = nagivator.Value;
                        var appended = AppendQueryStringCore(original, queryKey, queryValue);
                        nagivator.CurrentNode.SetAttributeValue(nagivator.LocalName, appended);
                    }
                }
            }

            return documentNode.OuterHtml;
        }

        /// <summary>
        /// Filter to exclude the specific html nodes
        /// Liquid syntax example:
        /// {%- if {{context.moniker}} != null -%}
        ///   {%- assign xpath = "//div[@class and not(contains(@class,'" | append: {{context.moniker}} | append: "'))]" -%}
        ///   {%- assign filter_content = {{content}} | exclude_nodes: {{xpath}} -%}
        ///   {{filter_content}}
        /// {%- endif -%}
        /// </summary>
        /// <param name="html">Html content to be filtered</param>
        /// <param name="nodesXPath">Nodes' XPath to be removed</param>
        /// <returns>String result</returns>
        public static string ExcludeNodes(string html, string nodesXPath)
        {
            // TODO: load html document once for multiple filters
            var htmlDocument = new HtmlDocument();
            try
            {
                htmlDocument.LoadHtml(html);
            }
            catch (Exception)
            {
                return html;
            }

            var node = htmlDocument.DocumentNode;
            var nodes = node.SelectNodes(nodesXPath);
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    n.Remove();
                }
            }
            return node.OuterHtml;
        }

        private static string AppendQueryStringCore(string original, string queryKey, string queryValue)
        {
            string originalWithoutBookmark = original;
            string? bookmark = null;
            var bookmarkIndex = original.IndexOf('#');
            if (bookmarkIndex >= 0)
            {
                bookmark = original.Substring(bookmarkIndex);
                originalWithoutBookmark = original.Remove(bookmarkIndex);
            }

            string queryString = string.Empty;
            string prefix = originalWithoutBookmark;
            var questionMarkIndex = originalWithoutBookmark.IndexOf('?');
            if (questionMarkIndex >= 0)
            {
                queryString = originalWithoutBookmark.Substring(questionMarkIndex);
                prefix = originalWithoutBookmark.Remove(questionMarkIndex);
            }

            // If query string already has the key, keep the original value.
            var query = HttpUtility.ParseQueryString(queryString);
            var originalQueriedValue = query.Get(queryKey);
            if (originalQueriedValue is null)
            {
                query[queryKey] = queryValue;
            }
            var result = $"{prefix}?{query}";

            return bookmark is null ? result : $"{result}{bookmark}";
        }
    }
}
