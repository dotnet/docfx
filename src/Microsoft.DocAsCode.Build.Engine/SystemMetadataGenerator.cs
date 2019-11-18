// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using CommonConstants = DataContracts.Common.Constants;

    internal sealed class SystemMetadataGenerator
    {
        private readonly IDocumentBuildContext _context;
        private readonly IEnumerable<FileInfo> _toc;

        public SystemMetadataGenerator(IDocumentBuildContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // Order toc files by the output folder depth
            _toc = context.GetTocInfo()
                .Select(s => new FileInfo(s.TocFileKey, context.GetFilePath(s.TocFileKey)))
                .Where(s => s.RelativePath != null)
                .OrderBy(s => s.RelativePath.SubdirectoryCount);
        }

        public SystemMetadata Generate(InternalManifestItem item)
        {
            var attrs = new SystemMetadata
            {
                Language = Constants.DefaultLanguage,
            };

            string key = GetFileKey(item.Key);
            attrs.Key = ((RelativePath)key).RemoveWorkingFolder();
            var file = (RelativePath)(item.FileWithoutExtension + item.Extension);

            attrs.RelativePathToRoot = (RelativePath.Empty).MakeRelativeTo(file);
            var fileWithoutWorkingFolder = file.RemoveWorkingFolder();
            attrs.Path = fileWithoutWorkingFolder;

            if (!string.IsNullOrEmpty(_context.VersionName))
            {
                attrs.VersionName = _context.VersionName;
                attrs.VersionFolder = _context.VersionFolder;
            }

            // 1. Root Toc is specified by RootTocKey, or by default in the top directory of output folder
            if (!string.IsNullOrEmpty(_context.RootTocPath))
            {
                attrs.RootTocKey = _context.RootTocPath;
                var rootTocPath = ((RelativePath)_context.RootTocPath).RemoveWorkingFolder();
                attrs.RootTocPath = rootTocPath;
                attrs.RelativePathToRootToc = rootTocPath.MakeRelativeTo(file);
            }
            else
            {
                GetRootTocFromOutputRoot(attrs, file);
            }

            if (item.DocumentType == CommonConstants.DocumentType.Toc)
            {
                // when item is toc, its toc is always itself
                attrs.TocPath = item.FileWithoutExtension + item.Extension;
                attrs.RelativePathToToc = System.IO.Path.GetFileName(item.FileWithoutExtension) + item.Extension;
                attrs.TocKey = item.Key;
                return attrs;
            }

            // 2. The algorithm of toc current article belongs to:
            //    a. If toc can be found in TocMap, return that toc
            //    b. Elsewise, get the nearest toc, **nearest** means nearest toc in **OUTPUT** folder
            var parentTocFiles = _context.GetTocFileKeySet(key)?.Select(s => new FileInfo(s, _context.GetFilePath(s)));
            var parentToc = GetNearestToc(parentTocFiles, file) ?? GetDefaultToc(key);

            if (parentToc != null)
            {
                var parentTocPath = parentToc.RelativePath.RemoveWorkingFolder();
                attrs.TocPath = parentTocPath;
                var tocRelativePath = parentTocPath.MakeRelativeTo(file);
                attrs.RelativePathToToc = tocRelativePath;
                attrs.TocKey = parentToc.Key;
                Logger.LogDiagnostic($"TOC file {parentTocPath} is found for {item.LocalPathFromRoot}.");
            }
            else
            {
                Logger.LogDiagnostic($"TOC file for {item.LocalPathFromRoot} is not found.");
            }

            return attrs;
        }

        private void GetRootTocFromOutputRoot(SystemMetadata attrs, RelativePath file)
        {
            var rootToc = _toc.FirstOrDefault();
            if (rootToc != null)
            {
                var rootTocPath = rootToc.RelativePath.RemoveWorkingFolder();
                if (rootTocPath.SubdirectoryCount == 0)
                {
                    attrs.RootTocPath = rootTocPath;
                    var rootTocRelativePath = rootTocPath.MakeRelativeTo(file);
                    attrs.RelativePathToRootToc = rootTocRelativePath;
                    attrs.RootTocKey = rootToc.Key;
                    Logger.LogDiagnostic($"Root TOC file {rootTocPath} is found.");
                }
                else
                {
                    Logger.LogDiagnostic(
                        $"Root TOC file from output folder is not found, the toppest TOC file is {rootTocPath}");
                }
            }
        }

        private FileInfo GetDefaultToc(string fileKey)
        {
            var outputPath = (RelativePath)_context.GetFilePath(fileKey);

            // MakeRelativeTo calculates how to get file "s" from "outputPath"
            // The standard for being the toc of current file is: Relative directory is empty or ".."s only
            var parentTocs = _toc
                .Select(s => new { rel = s.RelativePath.MakeRelativeTo(outputPath), info = s })
                .Where(s => s.rel.SubdirectoryCount == 0)
                .OrderBy(s => s.rel.ParentDirectoryCount)
                .Select(s => s.info);
            return parentTocs.FirstOrDefault();
        }

        /// <summary>
        /// return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md"
        /// </summary>
        private static FileInfo GetNearestToc(IEnumerable<FileInfo> tocFiles, RelativePath file)
        {
            if (tocFiles == null)
            {
                return null;
            }
            return (from toc in tocFiles
                    where toc.RelativePath != null
                    let relativePath = toc.RelativePath.RemoveWorkingFolder() - file
                    orderby relativePath.SubdirectoryCount, relativePath.ParentDirectoryCount, toc.FilePath, toc.Key
                    select toc)
                .FirstOrDefault();
        }

        private static string GetFileKey(string key)
        {
            if (key.StartsWith(RelativePath.NormalizedWorkingFolder, StringComparison.Ordinal)) return key;
            return RelativePath.NormalizedWorkingFolder + key;
        }

        private sealed class FileInfo
        {
            public string Key { get; set; }

            public string FilePath { get; set; }

            public RelativePath RelativePath{ get; set; }

            public FileInfo(string key, string filePath)
            {
                Key = key;
                FilePath = filePath;
                RelativePath = (RelativePath)filePath;
            }
        }
    }
}
