// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;

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
            using (new LoggerFileScope(wrapper.File.File))
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

            // HomepageUid and Uid is deprecated, unified to TopicUid
            if (string.IsNullOrEmpty(item.TopicUid))
            {
                if (!string.IsNullOrEmpty(item.Uid))
                {
                    item.TopicUid = item.Uid;
                    item.Uid = null;
                }
                else if (!string.IsNullOrEmpty(item.HomepageUid))
                {
                    item.TopicUid = item.HomepageUid;
                    Logger.LogWarning($"HomepageUid is deprecated in TOC. Please use topicUid to specify uid {item.Homepage}");
                    item.HomepageUid = null;
                }
            }
            // Homepage is deprecated, unified to TopicHref
            if (!string.IsNullOrEmpty(item.Homepage))
            {
                if (string.IsNullOrEmpty(item.TopicHref))
                {
                    item.TopicHref = item.Homepage;
                }
                else
                {
                    Logger.LogWarning($"Homepage is deprecated in TOC. Homepage {item.Homepage} is overwritten with topicHref {item.TopicHref}");
                }
            }

            // TocHref supports 2 forms: absolute path and local toc file.
            // When TocHref is set, using TocHref as Href in output, and using Href as Homepage in output
            var tocHrefType = Utility.GetHrefType(item.TocHref);
            if (!string.IsNullOrEmpty(item.TocHref))
            {
                if (!string.IsNullOrEmpty(item.Homepage))
                {
                    throw new DocumentException(
                        $"TopicHref should be used to specify the homepage for {item.TocHref} when tocHref is used.");
                }
                if (tocHrefType == HrefType.RelativeFile || tocHrefType == HrefType.RelativeFolder)
                {
                    throw new DocumentException($"TocHref {item.TocHref} only supports absolute path or local toc file.");
                }
            }

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
                        if (string.IsNullOrEmpty(item.TopicHref) && string.IsNullOrEmpty(item.TopicUid))
                        {
                            var defaultItem = GetDefaultHomepageItem(item);
                            if (defaultItem != null)
                            {
                                item.AggregatedHref = defaultItem.Href;
                                item.AggregatedUid = defaultItem.TopicUid;
                            }
                        }
                    }
                    // for backward compatibility
                    if (item.Href == null && item.Homepage == null)
                    {
                        item.Href = item.TocHref;
                        item.Homepage = item.TopicHref;
                    }
                    // check whether toc exists
                    if (!string.IsNullOrEmpty(item.TocHref) &&
                        (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile))
                    {
                        var tocFilePath = (RelativePath)file.File + (RelativePath)item.TocHref;
                        var tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);
                        if (!_collection.ContainsKey(tocFile))
                        {
                            var message =
                                $"Unable to find {item.TocHref}. Make sure the file is included in config file docfx.json!";
                            Logger.LogWarning(message);
                        }
                    }
                    break;
                case HrefType.RelativeFolder:
                    {
                        TocItemInfo tocFileModel;
                        if (!string.IsNullOrEmpty(item.TocHref) && (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile))
                        {
                            Logger.LogWarning($"Href {item.Href} is overwritten by tocHref {item.TocHref}");
                            var tocFilePath = (RelativePath)file.File + (RelativePath)item.TocHref;

                            var tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);

                            if (!_collection.TryGetValue(tocFile, out tocFileModel))
                            {
                                var message =
                                    $"Unable to find {item.TocHref}. Make sure the file is included in config file docfx.json!";
                                Logger.LogWarning(message);
                                break;
                            }
                        }
                        else
                        {
                            var relativeFolder = (RelativePath)file.File + (RelativePath)item.Href;
                            var tocFilePath = relativeFolder + (RelativePath)Constants.YamlTocFileName;

                            var tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);

                            // First, try finding toc.yml under the relative folder
                            // Second, try finding toc.md under the relative folder
                            if (!_collection.TryGetValue(tocFile, out tocFileModel))
                            {
                                tocFilePath = relativeFolder + (RelativePath)Constants.MarkdownTocFileName;
                                tocFile = new FileAndType(file.BaseDir, tocFilePath, file.Type, file.PathRewriter);
                                if (!_collection.TryGetValue(tocFile, out tocFileModel))
                                {
                                    var message =
                                        $"Unable to find either {Constants.YamlTocFileName} or {Constants.MarkdownTocFileName} inside {item.Href}. Make sure the file is included in config file docfx.json!";
                                    Logger.LogWarning(message);
                                    break;
                                }
                            }

                            item.TocHref = tocFilePath - (RelativePath)file.File;
                        }

                        // Get homepage from the referenced toc
                        if (string.IsNullOrEmpty(item.TopicHref) && string.IsNullOrEmpty(item.TopicUid))
                        {
                            stack.Push(file);
                            var resolved = ResolveItem(tocFileModel, stack).Content;
                            stack.Pop();
                            item.Href = item.TopicHref = resolved.TopicHref ?? resolved.AggregatedHref;
                            item.TopicUid = resolved.TopicUid ?? resolved.AggregatedUid;
                        }
                        else
                        {
                            item.Href = item.TopicHref;
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
                        item.Href = item.TopicHref;
                        item.Items = referencedToc.Items;
                    }
                    break;
                default:
                    break;
            }

            var relativeToFile = (RelativePath)file.File;

            item.Href = NormalizeHref(item.Href, relativeToFile);
            item.TocHref = NormalizeHref(item.TocHref, relativeToFile);
            item.TopicHref = NormalizeHref(item.TopicHref, relativeToFile);
            item.Homepage = NormalizeHref(item.Homepage, relativeToFile);

            wrapper.IsResolved = true;
            return wrapper;
        }

        private string NormalizeHref(string href, RelativePath relativeToFile)
        {
            if (!Utility.IsSupportedRelativeHref(href))
            {
                return href;
            }

            return (relativeToFile + (RelativePath)href).GetPathFromWorkingFolder();
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
            if (!string.IsNullOrEmpty(tocItem.TopicUid))
            {
                return true;
            }

            var hrefType = Utility.GetHrefType(tocItem.Href);
            if (hrefType == HrefType.RelativeFile)
            {
                return true;
            }

            return false;
        }
    }
}
