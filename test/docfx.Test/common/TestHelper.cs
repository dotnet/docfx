// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    internal static class TestHelper
    {
        public static TheoryData<string, int> FindTestSpecs(string path)
        {
            var result = new TheoryData<string, int>();

            Parallel.ForEach(
                Directory.EnumerateFiles(Path.Combine("specs", path), "*.yml", SearchOption.AllDirectories),
                file =>
                {
                    var i = 0;
                    foreach (var header in FindTestSpecHeadersInFile(file))
                    {
                        var name = $"{i + 1:D2}. {header}";
                        var folder = Path.Combine(
                            file.Replace("\\", "/").Replace($"specs/", "").Replace(".yml", ""),
                            name).Replace("\\", "/");

                        result.Add(folder, i++);
                    }
                });

            return result;
        }

        public static (string docsetPath, TestSpec spec) CreateDocset(string specName, int ordinal)
        {
            var i = specName.LastIndexOf('/');
            var specPath = specName.Substring(0, i) + ".yml";
            var sections = File.ReadAllText(Path.Combine("specs", specPath)).Split("\n---", StringSplitOptions.RemoveEmptyEntries);
            var yaml = sections[ordinal].Trim('\r', '\n', '-');
            var spec = YamlUtility.Deserialize<TestSpec>(yaml);
            var docsetPath = Path.Combine("specs.drop", specName.Replace("<", "").Replace(">", ""));

            if (Directory.Exists(docsetPath))
            {
                Directory.Delete(docsetPath, recursive: true);
            }

            foreach (var (file, content) in spec.Inputs)
            {
                var filePath = Path.Combine(docsetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, content);
            }

            return (docsetPath, spec);
        }

        public static void VerifyJsonContainEquals(JToken expected, JToken actual, string parentKey = null)
        {
            if (expected is JArray expectedArray)
            {
                Assert.IsType<JArray>(actual);
                var actualArray = (JArray)actual;
                Assert.Equal(expectedArray.Count, actualArray.Count);
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
                    Assert.True(actualObject.ContainsKey(key), $"Key '{key}' expected: {actual}");
                    VerifyJsonContainEquals(value, actualObject[key], key.ToString());
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
                    Assert.Equal(NormalizeHtml(expectedHtml), NormalizeHtml(actualHtml));
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

        private static IEnumerable<string> FindTestSpecHeadersInFile(string path)
        {
            var sections = File.ReadAllText(path).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                var header = YamlUtility.ReadHeader(yaml) ?? "";

                foreach (var ch in Path.GetInvalidPathChars())
                {
                    header = header.Replace(ch, ' ');
                }

                yield return header.Replace('/', ' ').Replace('\\', ' ');
            }
        }
    }
}
