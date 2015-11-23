// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Utility;
    using System.IO;
    using System.Linq;

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
                var cwd = Path.Combine(baseDirectory, item.CurrentWorkingDirectory ?? string.Empty);
                var options = GetMatchOptionsFromItem(item);
                var files = FileGlob.GetFiles(cwd, item.Files, item.Exclude, options).ToArray();
                if (files.Length == 0)
                {
                    Logger.LogInfo($"No files are found with glob pattern {item.Files.ToDelimitedString() ?? "<none>"}, excluding {item.Exclude.ToDelimitedString() ?? "<none>"}, under working directory {baseDirectory ?? "<current>"}");
                }
                expandedFileMapping.Add(
                    new FileMappingItem
                    {
                        CurrentWorkingDirectory = cwd,
                        Files = new FileItems(files),
                    });
            }

            expandedFileMapping.Expanded = true;
            return expandedFileMapping;
        }

        private static GlobMatcherOptions GetMatchOptionsFromItem(FileMappingItem item)
        {
            GlobMatcherOptions options = item.CaseSensitive ? GlobMatcherOptions.CaseSensitive : GlobMatcherOptions.IgnoreCase;
            if (item.AllowDotMatch) options |= GlobMatcherOptions.AllowDotMatch;
            if (item.DisableEscape) options |= GlobMatcherOptions.DisableEscape;
            if (item.DisableExpand) options |= GlobMatcherOptions.DisableExpand;
            if (item.DisableGlobStar) options |= GlobMatcherOptions.DisableGlobStar;
            if (item.DisableNegate) options |= GlobMatcherOptions.DisableNegate;
            return options;
        }
    }
}
