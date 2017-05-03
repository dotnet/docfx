// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.DataContracts.Common;

    public class TocGenerator
    {
        private const string JsonExtension = ".json";
        private const string HtmlExtension = ".html";
        private readonly Manifest _manifest;

        public string RootTocJsonRelativePath { get; }

        public string RootTocHtmlRelativePath { get; }

        public TocGenerator(Manifest manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            var rootToc =
                (from p in manifest.Files
                 where IsType(p.DocumentType, ManifestItemType.Toc)
                 select new
                 {
                     item = p,
                     jsonFilePath = GetOutputRelativePath(p, JsonExtension),
                     jsonFileRelativePath = (RelativePath)GetOutputRelativePath(p, JsonExtension),
                     htmlFilePath = GetOutputRelativePath(p, HtmlExtension),
                 } into s
                 orderby s.jsonFileRelativePath.SubdirectoryCount
                 select s).FirstOrDefault();

            RootTocJsonRelativePath = rootToc.jsonFilePath;
            RootTocHtmlRelativePath = rootToc.htmlFilePath;
        }

        private string GetOutputRelativePath(ManifestItem mi, string type)
        {
            if (mi.OutputFiles == null || !mi.OutputFiles.TryGetValue(type, out OutputFileInfo val))
            {
                return null;
            }

            return val?.RelativePath;
        }

        public TocItemViewModel GenerateTableOfContent(string basePath)
        {
            if (RootTocJsonRelativePath == null)
            {
                // TOC does not exist, generate table of content based on the content files, grouped by type, ordered by file path
                return ConstructTableOfContent(_manifest);
            }

            var rootTocItem = LoadFromFilePath<TocItemViewModel>(Path.Combine(basePath, RootTocJsonRelativePath));
            return MergeTableOfContent(rootTocItem, (RelativePath)RootTocJsonRelativePath, basePath, null);
        }

        private sealed class TocInfo
        {
            public string JsonFilePath { get; set; }
            public RelativePath JsonFileRelativePath { get; set; }
        }

        private T LoadFromFilePathQuietly<T>(string filePath)
        {
            try
            {
                return LoadFromFilePath<T>(filePath);
            }
            catch
            {
                return default(T);
            }
        }

        private T LoadFromFilePath<T>(string filePath)
        {
            using (var fs = EnvironmentContext.FileAbstractLayer.OpenRead(filePath))
            using (var sr = new StreamReader(fs))
            {
                return JsonUtility.Deserialize<T>(sr);
            }
        }

        private TocItemViewModel MergeTableOfContent(TocItemViewModel toc, RelativePath tocRelativePath, string basePath, RelativePath parentPath)
        {
            if (toc == null)
            {
                return null;
            }

            if (parentPath != null && toc.Href != null)
            {
                toc.Href = parentPath + (RelativePath)toc.Href; // Resolve toc to be relative to parent
            }

            if (toc.TocHref != null)
            {
                var tocHref = (RelativePath)Path.ChangeExtension(toc.TocHref, JsonExtension);
                var path = tocRelativePath + tocHref;
                var subToc = LoadFromFilePath<TocItemViewModel>(Path.Combine(basePath, path));

                var tocItem = MergeTableOfContent(subToc, path, basePath, tocHref);
                if (toc.Items != null)
                {
                    if (tocItem != null)
                    {
                        toc.Items.AddRange(tocItem.Items ?? Enumerable.Empty<TocItemViewModel>());
                    }
                }
                else
                {
                    toc.Items = tocItem.Items;
                }
            }

            if (toc.Items != null)
            {
                foreach (var item in toc.Items)
                {
                    MergeTableOfContent(item, tocRelativePath, basePath, parentPath);
                }
            }

            return toc;
        }

        private TocItemViewModel ConstructTableOfContent(Manifest manifest)
        {
            var fileGroups =
                from file in manifest.Files
                where IsType(file.DocumentType, ManifestItemType.Content)
                group file by file.DocumentType into g
                orderby g.Key
                select g;

            var toc = new TocItemViewModel
            {
                Items = new TocViewModel(),
            };
            foreach(var group in fileGroups)
            {
                var subtoc = new TocItemViewModel
                {
                    Name = group.Key,
                    Items = new TocViewModel(),
                };

                var subGroups = from i in @group
                                select new
                                {
                                    item = i,
                                    rel = i.OutputFiles[".html"].RelativePath,
                                    tocItem = new TocItemViewModel
                                    {
                                         Name = Path.GetFileNameWithoutExtension(i.OutputFiles[".html"].RelativePath),
                                         Href = i.OutputFiles[".html"].RelativePath
                                    }
                                }
                                into item
                                orderby item.rel
                                select item.tocItem;

                toc.Items.AddRange(subGroups);
            }

            return toc;
        }

        private List<string> FindTocInManifest(Manifest manifest)
        {
            // 1. List all the TOC files
            // 2. Get Root TOC, parse.
            // 3. Get files not included in TOC
            return manifest.Files.Where(p => IsType(p.DocumentType, ManifestItemType.Toc)).Select(p => new
            {
                item = p,
                jsonFile = p.OutputFiles[".json"].RelativePath,
                htmlFile = p.OutputFiles[".html"].RelativePath,
                jsonFileRelativePath = (RelativePath)p.OutputFiles[".json"].RelativePath,
            })
                .OrderBy(s => s.jsonFileRelativePath.SubdirectoryCount).Select(s => s.ToString())
                .ToList();
        }

        private bool IsType(string documentType, ManifestItemType type)
        {
            if (documentType.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (type == ManifestItemType.Content)
            {
                return !IsType(documentType, ManifestItemType.Resource) && !IsType(documentType, ManifestItemType.Toc);
            }

            return false;
        }

        private enum ManifestItemType
        {
            Content,
            Resource,
            Toc
        }
    }
}
