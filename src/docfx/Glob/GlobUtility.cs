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
    using Microsoft.DocAsCode.Utility;

    internal class GlobUtility
    {
        public static FileMapping ExpandFileMapping(string baseDirectory, FileMapping fileMapping)
        {
            if (fileMapping == null)
            {
                return null;
            }

            if (fileMapping.Expanded) return fileMapping;

            var expandedFileMapping = new FileMapping();
            foreach (var item in fileMapping.Items)
            {
                // Use local variable to avoid different items influencing each other
                var src = Path.Combine(baseDirectory, item.SourceFolder ?? string.Empty);
                var options = GetMatchOptionsFromItem(item);
                var files = FileGlob.GetFiles(src, item.Files, item.Exclude, options).ToArray();
                if (files.Length == 0)
                {
                    var currentSrcFullPath = string.IsNullOrEmpty(src) ? Directory.GetCurrentDirectory() : Path.GetFullPath(src);
                    Logger.LogInfo($"No files are found with glob pattern {item.Files.ToDelimitedString() ?? "<none>"}, excluding {item.Exclude.ToDelimitedString() ?? "<none>"}, under directory \"{currentSrcFullPath}\"");
                    CheckPatterns(item.Files);
                }
                expandedFileMapping.Add(
                    new FileMappingItem
                    {
                        SourceFolder = src,
                        Files = new FileItems(files),
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
                Logger.LogWarning("NOTE that `../` is currently not supported in glob pattern, please use `../` in `cwd` option instead.");
            }
        }
    }
}
