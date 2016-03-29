// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildTocDocument : BaseDocumentBuildStep
    {
        public override string Name => nameof(BuildTocDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            var toc = (TocItemViewModel)model.Content;
            Normalize(toc, model, host);
            // todo : metadata.
        }

        private void Normalize(TocItemViewModel item, FileModel model, IHostService hostService)
        {
            if (item == null) return;
            ValidateToc(item, model, hostService);
            var relativeToFile = (RelativePath)model.File;
            item.Href = NormalizeHref(item.Href, relativeToFile);
            item.OriginalHref = NormalizeHref(item.OriginalHref, relativeToFile);
            item.Homepage = NormalizeHref(item.Homepage, relativeToFile);
            if (item.Items != null)
            {
                foreach(var i in item.Items)
                {
                    Normalize(i, model, hostService);
                }
            }
        }

        private void ValidateToc(TocItemViewModel item, FileModel model, IHostService hostService)
        {
            if (!PathUtility.IsRelativePath(item.Href)) return;
            var file = model.File;

            FileAndType originalTocFile = null;

            string fileName = Path.GetFileName(item.Href);
            // Special handle for folder ends with '/'
            if (string.IsNullOrEmpty(fileName))
            {
                var href = item.Href + "toc.yml";
                var absHref = (RelativePath)file + (RelativePath)href;
                string tocPath = absHref.GetPathFromWorkingFolder();
                if (!hostService.SourceFiles.TryGetValue(tocPath, out originalTocFile))
                {
                    href = item.Href + "toc.md";
                    absHref = (RelativePath)file + (RelativePath)href;
                    tocPath = absHref.GetPathFromWorkingFolder();
                    if (!hostService.SourceFiles.TryGetValue(tocPath, out originalTocFile))
                    {
                        var message = $"Unable to find either toc.yml or toc.md inside {item.Href}. Make sure the file is included in config file docfx.json!";
                        Logger.LogWarning(message, file: model.LocalPathFromRepoRoot);
                        return;
                    }
                }

                Logger.LogInfo($"TOC file {href} inside {item.Href} is used", file: model.LocalPathFromRepoRoot);
                item.Href = href;
                item.OriginalHref = item.Href;
            }

            // Set default homepage
            SetDefaultHomepage(item, originalTocFile, model);
        }

        private string NormalizeHref(string file, RelativePath relativeToFile)
        {
            if (!PathUtility.IsRelativePath(file)) return file;
            return (relativeToFile + (RelativePath)file).GetPathFromWorkingFolder();
        }

        private void SetDefaultHomepage(TocItemViewModel item, FileAndType originalTocFile, FileModel model)
        {
            if (!string.IsNullOrEmpty(item.Homepage) || originalTocFile == null) return;
            var subTocPath = Path.Combine(originalTocFile.BaseDir, originalTocFile.File);
            var subToc = LoadSingleToc(subTocPath);
            var tocItem = GetDefaultHomepageItem(subToc);

            if (tocItem == null)
            {
                var error = $"Unable to get default page for {item.Href}: no item containing relative file link is defined inside TOC {subTocPath}";
                Logger.LogError(error, file: model.LocalPathFromRepoRoot);
                throw new DocumentException(error);
            }

            if (!string.IsNullOrEmpty(tocItem.Uid))
            {
                item.HomepageUid = tocItem.Uid;
            }
            else
            {
                item.Homepage = ((RelativePath)originalTocFile.File) + ((RelativePath)tocItem.Href);
            }
        }

        private TocItemViewModel GetDefaultHomepageItem(TocViewModel toc)
        {
            foreach (var item in toc)
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

        private TocViewModel LoadSingleToc(string filePath)
        {
            if ("toc.md".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return MarkdownTocReader.LoadToc(File.ReadAllText(filePath), filePath);
            }
            else if ("toc.yml".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return YamlUtility.Deserialize<TocViewModel>(filePath);
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file, supported toc files could be \"toc.md\" or \"toc.yml\".");
        }
    }
}
