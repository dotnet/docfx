// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using HtmlAgilityPack;

    public class YamlHtmlPart
    {
        public string OriginHtml { get; set; }

        public string Conceptual { get; set; }

        public string SourceFile { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;

        public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;

        public ImmutableDictionary<string, object> YamlHeader { get; set; } = ImmutableDictionary<string, object>.Empty;

        public static IList<YamlHtmlPart> SplitYamlHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var parts = new List<YamlHtmlPart>();

            var nodes = doc.DocumentNode.SelectNodes("//yamlheader");
            if (nodes == null) return parts;

            foreach (var node in nodes)
            {
                var sourceFile = node.GetAttributeValue("sourceFile", "NotFound");
                var startLine = node.GetAttributeValue("start", -1);
                var endLine = node.GetAttributeValue("end", -1);

                parts.Add(new YamlHtmlPart { SourceFile = sourceFile, StartLine = startLine, EndLine = endLine });
            }

            var startIndexes = nodes.Select(node => node.StreamPosition).Skip(1).ToList();
            startIndexes.Add(html.Length);
            var endIndexes = nodes.Select(node => node.StreamPosition + node.OuterHtml.Length - 1).ToList();

            for (var i = 0; i < parts.Count; i++)
            {
                if (i == 0)
                {
                    parts[i].OriginHtml = html.Substring(0, startIndexes[0]);
                }
                else
                {
                    parts[i].OriginHtml = html.Substring(startIndexes[i-1], startIndexes[i] - startIndexes[i-1]);
                }
                parts[i].Conceptual = html.Substring(endIndexes[i] + 1, startIndexes[i] - endIndexes[i] - 1);
            }

            return parts;
        }
    }
}
