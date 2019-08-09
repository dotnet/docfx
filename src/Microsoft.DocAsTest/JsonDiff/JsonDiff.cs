// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsTest
{
    public class JsonDiff
    {
        private static readonly JsonSerializer s_serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };

        private readonly JsonDiffNormalize[] _rules = Array.Empty<JsonDiffNormalize>();

        public JsonDiff() { }

        internal JsonDiff(JsonDiffNormalize[] rules) => _rules = rules;

        public void Verify(object expected, object actual, JsonDiffOptions options = default)
        {
            var diff = Diff(expected, actual, options);
            if (!string.IsNullOrEmpty(diff))
            {
                throw new JsonDiffException(diff);
            }
        }

        public string Diff(object expected, object actual, JsonDiffOptions options = default)
        {
            var expectedJson = ToJToken(expected);
            var actualJson = ToJToken(actual);

            var (expectedNorm, actualNorm) = Normalize(expectedJson, actualJson, options);
            var expectedText = Prettify(expectedNorm);
            var actualText = Prettify(actualNorm);

            var diff = new InlineDiffBuilder(new Differ())
                .BuildDiffModel(expectedText, actualText, ignoreWhitespace: true);

            var diffText = new StringBuilder();
            var hasDiff = false;

            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        diffText.Append('+');
                        hasDiff = true;
                        break;
                    case ChangeType.Deleted:
                        diffText.Append('-');
                        hasDiff = true;
                        break;
                    default:
                        diffText.Append(' ');
                        break;
                }

                diffText.AppendLine(line.Text);
            }

            return hasDiff ? diffText.ToString() : "";
        }

        public (JToken, JToken) Normalize(JToken expected, JToken actual, JsonDiffOptions options = null)
        {
            return NormalizeCore(expected, actual, "", options ?? JsonDiffOptions.Default);
        }

        public static string NormalizeHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var sb = new StringBuilder();
            NormalizeHtml(sb, doc.DocumentNode, 0);
            return sb.ToString().Trim();
        }

        private (JToken, JToken) NormalizeCore(JToken expected, JToken actual, string name, JsonDiffOptions options = default)
        {
            if (ApplyRules(expected, actual, name, out var expectedResult, out var actualResult))
            {
                return (expectedResult, actualResult);
            }

            switch (expected)
            {
                case JObject expectedObj when actual is JObject actualObj:
                    var expectedProps = new List<JProperty>(expectedObj.Count);
                    var actualProps = new List<JProperty>(actualObj.Count);

                    foreach (var prop in expectedObj)
                    {
                        var actualValue = actualObj.GetValue(prop.Key) ?? JValue.CreateUndefined();
                        var (expectedProp, actualProp) = NormalizeCore(prop.Value, actualValue, prop.Key, options);

                        if (expectedProp.Type != JTokenType.Undefined)
                        {
                            expectedProps.Add(new JProperty(prop.Key, expectedProp));
                        }
                        if (actualProp.Type != JTokenType.Undefined)
                        {
                            actualProps.Add(new JProperty(prop.Key, actualProp));
                        }
                    }

                    if (options.AdditionalProperties)
                    {
                        return (new JObject(expectedProps), new JObject(actualProps));
                    }

                    foreach (var additionalProperty in actualObj.Properties())
                    {
                        if (!expectedObj.TryGetValue(additionalProperty.Name, out _))
                        {
                            actualProps.Add(additionalProperty);
                        }
                    }

                    return (new JObject(expectedProps.OrderBy(p => p.Name)),
                            new JObject(actualProps.OrderBy(p => p.Name)));

                case JArray expectedArray when actual is JArray actualArray:
                    var expectedArrayResult = new JArray(expectedArray);
                    var actualArrayResult = new JArray(actualArray);
                    var length = Math.Min(expectedArray.Count, actualArray.Count);

                    for (var i = 0; i < length; i++)
                    {
                        var (expectedNorm, actualNorm) = NormalizeCore(expectedArray[i], actualArray[i], "", options);

                        expectedArrayResult[i] = expectedNorm;
                        actualArrayResult[i] = actualNorm;
                    }
                    return (expectedArrayResult, actualArrayResult);

                default:
                    return (expected, actual);
            }
        }

        private bool ApplyRules(JToken expected, JToken actual, string name, out JToken expectedResult, out JToken actualResult)
        {
            var match = false;

            expectedResult = default;
            actualResult = default;

            foreach (var rule in _rules)
            {
                (expectedResult, actualResult) = rule(expected, actual, name, this);

                if (!ReferenceEquals(expectedResult, expected) || !ReferenceEquals(actualResult, actual))
                {
                    match = true;
                }

                expected = expectedResult ?? JValue.CreateNull();
                actual = actualResult ?? JValue.CreateNull();
            }

            return match;
        }

        private static string Prettify(JToken token)
        {
            return token.ToString(Formatting.Indented)
                        .Replace(@"\r", "")
                        .Replace(@"\n", "\n")
                        .Replace("{}", "{\n}");
        }

        private static JToken ToJToken(object obj)
        {
            switch (obj)
            {
                case null:
                    return JValue.CreateNull();

                case JToken token:
                    return token;

                default:
                    return JToken.FromObject(obj, s_serializer);
            }
        }

        private static void NormalizeHtml(StringBuilder sb, HtmlNode node, int level)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Document:
                    foreach (var child in node.ChildNodes)
                    {
                        NormalizeHtml(sb, child, level);
                    }
                    break;

                case HtmlNodeType.Text:
                    var line = TrimWhiteSpace(node.InnerHtml);
                    if (!string.IsNullOrEmpty(line))
                    {
                        Indent();
                        sb.Append(line);
                        sb.Append("\n");
                    }
                    break;

                case HtmlNodeType.Element:
                    Indent();
                    sb.Append("<");
                    sb.Append(node.Name);

                    foreach (var attr in node.Attributes.OrderBy(a => a.Name))
                    {
                        sb.Append($" {attr.Name}=\"{TrimWhiteSpace(attr.Value)}\"");
                    }
                    sb.Append(">\n");

                    foreach (var child in node.ChildNodes)
                    {
                        NormalizeHtml(sb, child, level + 1);
                    }

                    Indent();
                    sb.Append($"</{node.Name}>\n");
                    break;
            }

            void Indent()
            {
                for (var i = 0; i < level; i++)
                    sb.Append("  ");
            }

            string TrimWhiteSpace(string text)
            {
                return Regex.Replace(text, @"\s+", " ").Trim();
            }
        }
    }
}
