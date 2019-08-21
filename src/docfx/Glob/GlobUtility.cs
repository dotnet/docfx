// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Glob;


    internal class GlobUtility
    {
        public static FileMapping ExpandFileMapping(string baseDirectory, FileMapping fileMapping)
        {
            if (fileMapping == null)
            {
                return null;
            }
            if (fileMapping.Expanded)
            {
                return fileMapping;
            }

            var expandedFileMapping = new FileMapping();
            foreach (var item in fileMapping.Items)
            {
                string expandedSourceFolder = Path.Combine(baseDirectory, Environment.ExpandEnvironmentVariables(item.SourceFolder ?? string.Empty));
                var options = GetMatchOptionsFromItem(item);
                var fileItems = new FileItems(FileGlob.GetFiles(expandedSourceFolder, item.Files, item.Exclude, options));
                if (fileItems.Count == 0)
                {
                    var currentSrcFullPath = string.IsNullOrEmpty(expandedSourceFolder) ? Directory.GetCurrentDirectory() : Path.GetFullPath(expandedSourceFolder);
                    Logger.LogInfo($"No files are found with glob pattern {StringExtension.ToDelimitedString(item.Files) ?? "<none>"}, excluding {StringExtension.ToDelimitedString(item.Exclude) ?? "<none>"}, under directory \"{currentSrcFullPath}\"");
                    CheckPatterns(item.Files);
                }
                if (item.Exclude != null)
                {
                    CheckPatterns(item.Exclude);
                }
                expandedFileMapping.Add(
                    new FileMappingItem
                    {
                        SourceFolder = expandedSourceFolder,
                        Files = fileItems,
                        DestinationFolder = item.DestinationFolder
                    });
            }

            expandedFileMapping.Expanded = true;
            return expandedFileMapping;
        }

        private static GlobMatcherOptions GetMatchOptionsFromItem(FileMappingItem item)
        {
            GlobMatcherOptions options = item?.CaseSensitive ?? false ? GlobMatcherOptions.None : GlobMatcherOptions.IgnoreCase;
            if (item?.AllowDotMatch ?? false) options |= GlobMatcherOptions.AllowDotMatch;
            if (!(item?.DisableEscape ?? false)) options |= GlobMatcherOptions.AllowEscape;
            if (!(item?.DisableExpand ?? false)) options |= GlobMatcherOptions.AllowExpand;
            if (!(item?.DisableGlobStar ?? false)) options |= GlobMatcherOptions.AllowGlobStar;
            if (!(item?.DisableNegate ?? false)) options |= GlobMatcherOptions.AllowNegate;
            return options;
        }

        private static void CheckPatterns(IEnumerable<string> patterns)
        {
            if (patterns.Any(s => s.Contains('\\')))
            {
                Logger.LogInfo("NOTE that `\\` in glob pattern is used as Escape character. DONOT use it as path separator.");
            }

            if (patterns.Any(s => s.Contains("../")))
            {
                Logger.LogWarning("NOTE that `../` is currently not supported in glob pattern, please use `../` in `src` option instead.");
            }
        }
    }
}
