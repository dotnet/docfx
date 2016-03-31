// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    internal sealed class TocResolver
    {
        private readonly IHostService _host;
        private readonly Dictionary<FileAndType, TocItemInfo> _collection;
        private readonly Dictionary<FileAndType, TocItemInfo> _notInProjectTocCache = new Dictionary<FileAndType, TocItemInfo>();
        public TocResolver(IHostService host, Dictionary<FileAndType, TocItemInfo> collection)
        {
            _host = host;
            _collection = collection;
        }

        public TocItemInfo Resolve(FileAndType file)
        {
            return ResolveItem(_collection[file], new Stack<FileAndType>());
        }

        private TocItemInfo ResolveItem(TocItemInfo wrapper, Stack<FileAndType> stack)
        {
            using (new LoggerFileScope(wrapper.File.FullPath))
            {
                return ResolveItemCore(wrapper, stack);
            }
        }

        private TocItemInfo ResolveItemCore(TocItemInfo wrapper, Stack<FileAndType> stack)
        {
            if (wrapper.IsResolved)
            {
                return wrapper;
            }

            var file = wrapper.File;
            if (stack.Contains(file))
            {
                throw new DocumentException($"Circular reference to {file.FullPath} is found in {stack.Peek().FullPath}");
            }

            var item = wrapper.Content;
            var hrefType = Utility.GetHrefType(item.Href);
            switch (hrefType)
            {
                case HrefType.AbsolutePath:
                case HrefType.RelativeFile:
                    if (item.Items != null && item.Items.Count > 0)
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            item.Items[i] = ResolveItem(new TocItemInfo(file, item.Items[i]), stack).Content;
                        }
                        if (string.IsNullOrEmpty(item.Homepage) && string.IsNullOrEmpty(item.HomepageUid))
                        {
                            var defaultItem = GetDefaultHomepageItem(item);
                            if (defaultItem != null)
                            {
                                item.Homepage = defaultItem.Href;
                                item.HomepageUid = defaultItem.Uid;
                            }
                        }
                    }

                    break;
                case HrefType.RelativeFolder:
                    {
                        var relativeFolder = (RelativePath)file.File + (RelativePath)item.Href;
                        var tocFilePath = relativeFolder + (RelativePath)Constants.YamlTocFileName;

                        var tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);
                        TocItemInfo tocFileModel;

                        // First, try finding toc.yml under the relative folder
                        // Second, try finding toc.md under the relative folder
                        if (!_collection.TryGetValue(tocFile, out tocFileModel))
                        {
                            tocFilePath = relativeFolder + (RelativePath)Constants.MarkdownTocFileName;
                            tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);
                            if (!_collection.TryGetValue(tocFile, out tocFileModel))
                            {
                                var message = $"Unable to find either {Constants.YamlTocFileName} or {Constants.MarkdownTocFileName} inside {item.Href}. Make sure the file is included in config file docfx.json!";
                                Logger.LogWarning(message);
                                break;
                            }
                        }

                        item.TocHref = tocFilePath;

                        // Get homepage from the referenced toc
                        if (string.IsNullOrEmpty(item.Homepage) && string.IsNullOrEmpty(item.HomepageUid))
                        {
                            stack.Push(file);
                            var resolved = ResolveItem(tocFileModel, stack).Content;
                            stack.Pop();
                            item.Href = resolved.Homepage;
                            item.Uid = resolved.HomepageUid;
                        }
                        else
                        {
                            // Set homepage to href
                            item.Href = item.Homepage;
                            item.Uid = item.HomepageUid;
                        }

                        if (item.Items != null)
                        {
                            for (int i = 0; i < item.Items.Count; i++)
                            {
                                item.Items[i] = ResolveItem(new TocItemInfo(file, item.Items[i]), stack).Content;
                            }
                        }
                    }
                    break;
                case HrefType.MarkdownTocFile:
                case HrefType.YamlTocFile:
                    {
                        var tocFilePath = (RelativePath)file.File + (RelativePath)item.Href;
                        var tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);
                        TocItemInfo tocFileModel;
                        TocItemViewModel referencedToc;
                        stack.Push(file);
                        if (_collection.TryGetValue(tocFile, out tocFileModel) || _notInProjectTocCache.TryGetValue(tocFile, out tocFileModel))
                        {
                            tocFileModel = ResolveItem(tocFileModel, stack);
                            tocFileModel.IsReferenceToc = true;
                            referencedToc = tocFileModel.Content;
                        }
                        else
                        {
                            // It is acceptable that the referenced toc file is not included in docfx.json, as long as it can be found locally
                            tocFileModel = new TocItemInfo(tocFile, new TocItemViewModel
                            {
                                Items = Utility.LoadSingleToc(tocFile.FullPath)
                            });

                            tocFileModel = ResolveItem(tocFileModel, stack);
                            referencedToc = tocFileModel.Content;
                            _notInProjectTocCache[tocFile] = tocFileModel;
                        }
                        stack.Pop();
                        // For referenced toc, content from referenced toc is expanded as the items of current toc item,
                        // Href is reset to the homepage of current toc item
                        item.Href = item.Homepage;
                        item.Uid = item.HomepageUid;
                        item.Items = referencedToc.Items;
                    }
                    break;
                default:
                    break;
            }

            var relativeToFile = (RelativePath)file.File;

            item.Href = NormalizeHref(item.Href, relativeToFile);
            item.TocHref = NormalizeHref(item.TocHref, relativeToFile);
            item.Homepage = NormalizeHref(item.Homepage, relativeToFile);
            wrapper.IsResolved = true;
            return wrapper;
        }

        private string NormalizeHref(string file, RelativePath relativeToFile)
        {
            if (!PathUtility.IsRelativePath(file)) return file;
            return (relativeToFile + (RelativePath)file).GetPathFromWorkingFolder();
        }

        private TocItemViewModel GetDefaultHomepageItem(TocItemViewModel toc)
        {
            if (toc == null || toc.Items == null)
            {
                return null;
            }

            foreach (var item in toc.Items)
            {
                var tocItem = TreeIterator.PreorderFirstOrDefault(item, s => s.Items, s => IsValidHomepageLink(s));
                if (tocItem != null)
                {
                    return tocItem;
                }
            }
            return null;
        }

        /// <summary>
        /// Valid homepage href should:
        /// 1. relative file path
        /// 2. refer to a file
        /// 3. folder is not supported
        /// 4. refer to an `uid`
        /// </summary>
        /// <param name="href"></param>
        /// <returns></returns>
        private bool IsValidHomepageLink(TocItemViewModel tocItem)
        {
            return !string.IsNullOrEmpty(tocItem.Uid) ||
             (PathUtility.IsRelativePath(tocItem.Href) && !string.IsNullOrEmpty(Path.GetFileName(tocItem.Href)));
        }
    }
}
