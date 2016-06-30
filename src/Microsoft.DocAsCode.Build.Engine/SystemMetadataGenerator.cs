// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    internal sealed class SystemMetadataGenerator
    {
        private readonly IDocumentBuildContext _context;
        private readonly IEnumerable<FileInfo> _toc;
        public SystemMetadataGenerator(IDocumentBuildContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;

            // Order toc files by the output folder depth
            _toc = context.GetTocInfo()
                .Select(s => new FileInfo(s.TocFileKey, (RelativePath)context.GetFilePath(s.TocFileKey)))
                .Where(s => s.File != null)
                .OrderBy(s => s.File.SubdirectoryCount);
        }

        public SystemMetadata Generate(InternalManifestItem item)
        {
            var attrs = new SystemMetadata
            {
                Language = Constants.DefaultLanguage,
            };

            string key = GetFileKey(item.Key);
            var file = (RelativePath)(item.FileWithoutExtension + item.Extension);

            attrs.RelativePathToRoot = (RelativePath.Empty).MakeRelativeTo(file);
            var fileWithoutWorkingFolder = file.RemoveWorkingFolder();
            attrs.Path = fileWithoutWorkingFolder;

            // 1. Root Toc is always in the top directory of output folder
            var rootToc = _toc.FirstOrDefault();
            if (rootToc != null)
            {
                var rootTocPath = rootToc.File.RemoveWorkingFolder();
                if (rootTocPath.SubdirectoryCount == 0)
                {
                    attrs.RootTocPath = rootTocPath;
                    var rootTocRelativePath = rootTocPath.MakeRelativeTo(file);
                    attrs.RelativePathToRootToc = rootTocRelativePath;
                    attrs.RootTocKey = rootToc.Key;
                    Logger.LogVerbose($"Root TOC file {rootTocPath} is found.");
                }
                else
                {
                    Logger.LogVerbose($"Root TOC file from output folder is not found, the toppest TOC file is {rootTocPath}");
                }
            }

            // 2. The algorithm of toc current article belongs to:
            //    a. If toc can be found in TocMap, return that toc
            //    b. Elsewise, get the nearest toc, **nearest** means nearest toc in **OUTPUT** folder
            var parentTocFiles = _context.GetTocFileKeySet(key)?.Select(s => new FileInfo(s, (RelativePath)_context.GetFilePath(s)));
            var parentToc = GetNearestToc(parentTocFiles);
            if (parentToc == null)
            {
                parentToc = GetDefaultToc(key);
            }

            if (parentToc != null)
            {
                var parentTocPath = parentToc.File.RemoveWorkingFolder();
                attrs.TocPath = parentTocPath;
                var tocRelativePath = parentTocPath.MakeRelativeTo(file);
                attrs.RelativePathToToc = tocRelativePath;
                attrs.TocKey = parentToc.Key;
                Logger.LogVerbose($"TOC file {parentTocPath} is found for {item.LocalPathFromRepoRoot}.");
            }
            else
            {
                Logger.LogVerbose($"TOC file for {item.LocalPathFromRepoRoot} is not found.");
            }

            return attrs;
        }

        private FileInfo GetDefaultToc(string fileKey)
        {
            var outputPath = (RelativePath)_context.GetFilePath(fileKey);

            // MakeRelativeTo calculates how to get file "s" from "outputPath"
            // The standard for being the toc of current file is: Relative directory is empty or ".."s only
            var parentTocs = _toc
                .Select(s => new { rel = s.File.MakeRelativeTo(outputPath), info = s })
                .Where(s => s.rel.SubdirectoryCount == 0)
                .OrderBy(s => s.rel.ParentDirectoryCount)
                .Select(s => s.info);
            return parentTocs.FirstOrDefault();
        }

        private static FileInfo GetNearestToc(IEnumerable<FileInfo> tocFiles)
        {
            // Get the deepest toc as default parent toc
            return tocFiles?
                .Where(s => s.File != null)
                .OrderByDescending(s => s.File.SubdirectoryCount)
                .FirstOrDefault();
        }

        private static string GetFileKey(string key)
        {
            if (key.StartsWith(RelativePath.NormalizedWorkingFolder)) return key;
            return RelativePath.NormalizedWorkingFolder + key;
        }

        private sealed class FileInfo
        {
            public string Key { get; set; }
            public RelativePath File { get; set; }
            public FileInfo(string key, RelativePath file)
            {
                Key = key;
                File = file;
            }
        }
    }
}
