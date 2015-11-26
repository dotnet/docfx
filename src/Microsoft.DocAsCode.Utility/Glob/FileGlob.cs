// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Utility;

    public class FileGlob
    {
        public static IEnumerable<string> GetFiles(string cwd, IEnumerable<string> patterns, IEnumerable<string> excludePatterns, GlobMatcherOptions options = GlobMatcherOptions.IgnoreCase)
        {
            // If there is no pattern, nothing will be included
            if (patterns == null) return Enumerable.Empty<string>();
            if (string.IsNullOrEmpty(cwd)) cwd = Environment.CurrentDirectory;

            IEnumerable<GlobMatcher> globList = patterns.Select(s => new GlobMatcher(s, options));
            IEnumerable<GlobMatcher> excludeGlobList = Enumerable.Empty<GlobMatcher>();
            if (excludePatterns != null)
                excludeGlobList = excludePatterns.Select(s => new GlobMatcher(s, options));
            return GetFilesCore(cwd, globList, excludeGlobList);
        }

        private static IEnumerable<string> GetFilesCore(string cwd, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            if (!Directory.Exists(cwd)) yield break;
            foreach (var file in GetFilesFromSubfolder(cwd, cwd, globs, excludeGlobs))
            {
                yield return file.ToNormalizedFullPath();
            }
        }

        private static IEnumerable<string> GetFilesFromSubfolder(string baseDirectory, string cwd, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            foreach (var file in Directory.GetFiles(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = PathUtility.MakeRelativePath(cwd, file);
                if (IsFileMatch(relativePath, globs, excludeGlobs))
                {
                    yield return file;
                }
            }

            foreach (var dir in Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = PathUtility.MakeRelativePath(cwd, dir);

                // For folder, exclude glob matches folder means nothing, e.g. **/a matches b/a folder, however, **/a does not match b/a/c file
                foreach (var glob in globs)
                {
                    if (glob.Match(relativePath, true))
                    {
                        foreach(var file in GetFilesFromSubfolder(dir, cwd, globs, excludeGlobs))
                        {
                            yield return file;
                        }

                        break;
                    }
                }
            }
        }

        private static bool IsFileMatch(string path, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            foreach (var exclude in excludeGlobs)
            {
                if (exclude.Match(path, false)) return false;
            }
            foreach (var glob in globs)
            {
                if (glob.Match(path, false)) return true;
            }
            return false;
        }
    }
}