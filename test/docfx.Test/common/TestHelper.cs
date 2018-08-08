// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    internal static class TestHelper
    {
        public static void VerifyJsonContainEquals(JToken expected, JToken actual, string parentKey = null)
        {
            if (expected is JArray expectedArray)
            {
                Assert.IsType<JArray>(actual);
                var actualArray = (JArray)actual;
                Assert.True(expectedArray.Count == actualArray.Count, $"expected: {expected}\n got: {actual}");
                for (var i = 0; i < expectedArray.Count; i++)
                {
                    VerifyJsonContainEquals(expectedArray[i], actualArray[i]);
                }
            }
            else if (expected is JObject expectedObject)
            {
                Assert.IsType<JObject>(actual);
                var actualObject = (JObject)actual;
                foreach (var (key, value) in expectedObject)
                {
                    if (key.StartsWith("!"))
                    {
                        Assert.False(actualObject.ContainsKey(key.Substring(1)), $"'{key.Substring(1)}' should not exist in {actual}");
                    }
                    else
                    {
                        Assert.True(actualObject.ContainsKey(key), $"Key '{key}' expected: {actual}");
                        VerifyJsonContainEquals(value, actualObject[key], key.ToString());
                    }
                }
            }
            else
            {
                var expectedValue = ((JValue)expected).Value;
                var actualValue = ((JValue)actual).Value;

                if (expectedValue is string expectedHtml && actualValue is string actualHtml &&
                    expectedHtml.StartsWith('<') && expectedHtml.EndsWith('>') && parentKey == "content")
                {
                    // Treat `content` as html if the expected value looks like: <blablabla>
                    Assert.Equal(TestHelper.NormalizeHtml(expectedHtml), TestHelper.NormalizeHtml(actualHtml));
                }
                else
                {
                    Assert.Equal(expectedValue, actualValue);
                }
            }
        }

        public static string NormalizeHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var sb = new StringBuilder();
            Walk(doc.DocumentNode, 0);
            return sb.ToString();

            void Walk(HtmlNode node, int level)
            {
                switch (node.NodeType)
                {
                    case HtmlNodeType.Document:
                        foreach (var child in node.ChildNodes)
                        {
                            Walk(child, level);
                        }
                        break;

                    case HtmlNodeType.Text:
                        var line = TrimWhiteSpace(node.InnerHtml);
                        if (!string.IsNullOrEmpty(line))
                        {
                            Indent(level);
                            sb.Append(line);
                            sb.Append("\n");
                        }
                        break;

                    case HtmlNodeType.Element:
                        Indent(level);
                        sb.Append("<");
                        sb.Append(node.Name);
                        foreach (var attr in node.Attributes.OrderBy(a => a.Name))
                        {
                            sb.Append($" {attr.Name}=\"{TrimWhiteSpace(attr.Value)}\"");
                        }
                        sb.Append(">\n");

                        foreach (var child in node.ChildNodes)
                        {
                            Walk(child, level + 1);
                        }

                        Indent(level);
                        sb.Append($"</{node.Name}>\n");
                        break;
                }
            }

            void Indent(int level)
            {
                for (var i = 0; i < level; i++)
                    sb.Append("  ");
            }

            string TrimWhiteSpace(string text)
            {
                return Regex.Replace(text, @"\s+", " ").Trim();
            }
        }

        public static void VerifyLogEquals(List<string> expectedLog, List<string> actualLog)
        {
            Assert.Equal(expectedLog.Count, actualLog.Count);

            for (var i = 0; i < expectedLog.Count; i++)
            {
                var (_, expectedLogItem) = JsonUtility.Deserialize<JArray>(expectedLog[i]);
                var (_, actualLogItem) = JsonUtility.Deserialize<JArray>(actualLog[i]);
                VerifyLogItemEquals(expectedLogItem, actualLogItem);
            }
        }

        private static void VerifyLogItemEquals(JArray expectedLogItem, JArray actualLogItem)
        {
            Assert.Equal(expectedLogItem.Count, actualLogItem.Count);
            for (var i = 0; i < expectedLogItem.Count; i++)
            {
                var expectedValue = (JValue)expectedLogItem[i];
                var actualValue = (JValue)actualLogItem[i];
                switch (expectedValue.Type)
                {
                    case JTokenType.String:
                        VerifyStringEqual(expectedValue, actualValue);
                        break;
                    default:
                        Assert.Equal(expectedValue, actualValue);
                        break;
                }
            }
        }

        private static void VerifyStringEqual(JValue expected, JValue actual)
        {
            Assert.Equal(JTokenType.String, actual.Type);
            var expectedStr = expected.ToString();
            var actualStr = actual.ToString();
            switch (expectedStr)
            {
                case "*":
                    break;
                default:
                    Assert.Equal(expectedStr, actualStr);
                    break;
            }
        }
    }
}
