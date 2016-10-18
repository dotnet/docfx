namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using HtmlAgilityPack;

    [Export(nameof(ValidateBookmark), typeof(IPostProcessor))]
    public class ValidateBookmark : IPostProcessor
    {
        private static readonly string XpathTemplate = "//*/@{0}";

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            var registeredBookmarks = new Dictionary<string, HashSet<string>>();
            var bookmarks = new Dictionary<string, List<Tuple<string, string>>>();
            var fileMapping = new Dictionary<string, string>();

            foreach (var p in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                         from output in item.OutputFiles
                                         where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                         select new
                                         {
                                             RelativePath = output.Value.RelativePath,
                                             SrcRelativePath = item.SourceRelativePath,
                                         })
            {
                string srcRelativePath = p.SrcRelativePath;
                string relativePath = p.RelativePath;
                var filePath = Path.Combine(outputFolder, relativePath);
                fileMapping[relativePath] = srcRelativePath;
                var html = new HtmlDocument();

                if (File.Exists(filePath))
                {
                    try
                    {
                        html.Load(filePath, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Warning: Can't load content from {filePath}: {ex.Message}");
                        continue;
                    }

                    var links = GetNodeAttribute(html, "src").Concat(GetNodeAttribute(html, "href"));
                    bookmarks[relativePath] = (from link in links
                                               let index = link.IndexOf("#")
                                               where index != -1 && PathUtility.IsRelativePath(link)
                                               select Tuple.Create(
                                                   HttpUtility.UrlDecode(link.Remove(index)),
                                                   link.Substring(index))).ToList();
                    var anchors = GetNodeAttribute(html, "id").Concat(GetNodeAttribute(html, "name"));
                    registeredBookmarks[relativePath] = new HashSet<string>(anchors);
                }
            }

            // validate bookmarks
            foreach (var item in bookmarks)
            {
                string path = item.Key;
                foreach (var b in item.Value)
                {
                    string linkedToFile = b.Item1 == string.Empty ? path : b.Item1;
                    string anchor = b.Item2;
                    HashSet<string> anchors;
                    if (registeredBookmarks.TryGetValue(linkedToFile, out anchors) && !anchors.Contains(anchor))
                    {
                        string currentFileSrc = fileMapping[path];
                        string linkedToFileSrc = fileMapping[linkedToFile];
                        Logger.LogWarning($"File {currentFileSrc} contains illegal link {linkedToFileSrc + anchor}: the file {linkedToFileSrc} doesn't contain a bookmark named {anchor}.");
                    }
                }
            }

            return manifest;
        }

        private static IEnumerable<string> GetNodeAttribute(HtmlDocument html, string attribute)
        {
            return html.DocumentNode.SelectNodes(string.Format(XpathTemplate, attribute)).Select(n => n.GetAttributeValue(attribute, null));
        }
    }
}
