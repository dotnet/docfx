// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    using HtmlAgilityPack;

    public static class MarkupUtility
    {
        private static readonly char[] UriFragmentOrQueryString = new char[] { '#', '?' };

        public static MarkupResult Parse(MarkupResult markupResult, FileAndType ft, ImmutableDictionary<string, FileAndType> sourceFiles)
        {
            return Parse(markupResult, ft.File, sourceFiles);
        }

        public static MarkupResult Parse(MarkupResult markupResult, string file, ImmutableDictionary<string, FileAndType> sourceFiles)
        {
            if (markupResult == null)
            {
                throw new ArgumentNullException(nameof(markupResult));
            }
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (sourceFiles == null)
            {
                throw new ArgumentNullException(nameof(sourceFiles));
            }
            return ParseCore(markupResult, file, sourceFiles);
        }

        private static MarkupResult ParseCore(MarkupResult markupResult, string file, ImmutableDictionary<string, FileAndType> sourceFiles)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(markupResult.Html);
            var result = markupResult.Clone();

            var node = doc.DocumentNode.SelectSingleNode("//yamlheader");
            if (node != null)
            {
                using (var sr = new StringReader(StringHelper.HtmlDecode(node.InnerHtml)))
                {
                    result.YamlHeader = YamlUtility.Deserialize<Dictionary<string, object>>(sr).ToImmutableDictionary();
                }
                node.Remove();
            }

            result.FileLinkSources = GetFileLinkSource(file, doc, sourceFiles);
            result.LinkToFiles = result.FileLinkSources.Keys.ToImmutableArray();

            result.UidLinkSources = GetUidLinkSources(doc);
            result.LinkToUids = result.UidLinkSources.Keys.ToImmutableHashSet();

            if (result.Dependency.Length > 0)
            {
                result.Dependency =
                    (from d in result.Dependency
                     select
                        ((RelativePath)file + (RelativePath)d)
                            .GetPathFromWorkingFolder()
                            .ToString()
                    ).ToImmutableArray();
            }
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                result.Html = sw.ToString();
            }
            return result;
        }


        private static ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> GetFileLinkSource(string currentFile, HtmlDocument doc, ImmutableDictionary<string, FileAndType> sourceFiles)
        {
            var fileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            foreach (var pair in (from n in doc.DocumentNode.Descendants()
                                  where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                                  from attr in n.Attributes
                                  where string.Equals(attr.Name, "src", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase)
                                  where !string.IsNullOrWhiteSpace(attr.Value)
                                  select new { Node = n, Attr = attr }).ToList())
            {
                string anchor = null;
                var link = pair.Attr;
                string linkFile = link.Value;
                var index = linkFile.IndexOfAny(UriFragmentOrQueryString);
                if (index != -1)
                {
                    anchor = linkFile.Substring(index);
                    linkFile = linkFile.Remove(index);
                }
                if (RelativePath.IsRelativePath(linkFile))
                {
                    var path = (RelativePath)currentFile + RelativePath.FromUrl(linkFile);
                    var file = path.GetPathFromWorkingFolder();
                    if (sourceFiles.ContainsKey(file))
                    {
                        string anchorInHref;
                        if (!string.IsNullOrEmpty(anchor) &&
                            string.Equals(link.Name, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            anchorInHref = anchor;
                        }
                        else
                        {
                            anchorInHref = null;
                        }

                        link.Value = file.UrlEncode().ToString() + anchorInHref;
                    }

                    if (!fileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
                    {
                        sources = new List<LinkSourceInfo>();
                        fileLinkSources[file] = sources;
                    }
                    sources.Add(new LinkSourceInfo
                    {
                        Target = file,
                        Anchor = anchor, // Actually this contains both query and bookmark
                        SourceFile = pair.Node.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Node.GetAttributeValue("sourceStartLineNumber", 0),
                    });
                }
            }
            return fileLinkSources.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableList());
        }

        private static ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> GetUidLinkSources(HtmlDocument doc)
        {
            var uidInXref =
                from n in doc.DocumentNode.Descendants()
                where string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                from attr in n.Attributes
                where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                select Tuple.Create(n, attr.Value);
            var uidInHref =
                from n in doc.DocumentNode.Descendants()
                where !string.Equals(n.Name, "xref", StringComparison.OrdinalIgnoreCase)
                from attr in n.Attributes
                where string.Equals(attr.Name, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(attr.Name, "uid", StringComparison.OrdinalIgnoreCase)
                where attr.Value.StartsWith("xref:", StringComparison.OrdinalIgnoreCase)
                select Tuple.Create(n, attr.Value.Substring("xref:".Length));
            return (from pair in uidInXref.Concat(uidInHref)
                    where !string.IsNullOrWhiteSpace(pair.Item2)
                    let queryIndex = pair.Item2.IndexOfAny(UriFragmentOrQueryString)
                    let targetUid = queryIndex == -1 ? pair.Item2 : pair.Item2.Remove(queryIndex)
                    select new LinkSourceInfo
                    {
                        Target = Uri.UnescapeDataString(targetUid),
                        SourceFile = pair.Item1.GetAttributeValue("sourceFile", null),
                        LineNumber = pair.Item1.GetAttributeValue("sourceStartLineNumber", 0),
                    } into lsi
                    group lsi by lsi.Target into g
                    select new KeyValuePair<string, ImmutableList<LinkSourceInfo>>(g.Key, g.ToImmutableList())).ToImmutableDictionary();
        }
    }
}
