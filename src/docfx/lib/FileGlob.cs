// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide Utilities of File glob
    /// </summary>
    public static class FileGlob
    {
        /// <summary>
        /// Get files with patterns
        /// </summary>
        public static IEnumerable<string> GetFiles(string cwd, IEnumerable<string> patterns, IEnumerable<string> excludePatterns, GlobMatcherOptions options = GlobMatcher.DefaultOptions)
        {
            // If there is no pattern, nothing will be included
            if (patterns == null)
            {
                return Enumerable.Empty<string>();
            }

            if (string.IsNullOrEmpty(cwd))
            {
                cwd = Directory.GetCurrentDirectory();
            }

            IEnumerable<GlobMatcher> globList = patterns.Select(s => new GlobMatcher(s, options));
            IEnumerable<GlobMatcher> excludeGlobList = Enumerable.Empty<GlobMatcher>();
            if (excludePatterns != null)
            {
                excludeGlobList = excludePatterns.Select(s => new GlobMatcher(s, options));
            }
            return GetFilesCore(cwd, globList, excludeGlobList);
        }

        private static IEnumerable<string> GetFilesCore(string cwd, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            if (!Directory.Exists(cwd))
            {
                yield break;
            }

            foreach (var file in GetFilesFromSubfolder(cwd, cwd, globs, excludeGlobs))
            {
                yield return PathUtility.NormalizeFile(file);
            }
        }

        private static IEnumerable<string> GetFilesFromSubfolder(string baseDirectory, string cwd, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            foreach (var file in Directory.GetFiles(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = PathUtility.GetRelativePathToFile(cwd, file);
                if (IsFileMatch(relativePath, globs, excludeGlobs))
                {
                    yield return file;
                }
            }

            foreach (var dir in Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = GetRelativeDirectoryPath(cwd, dir);
                if (IsDirectoryMatch(relativePath, globs, excludeGlobs))
                {
                    foreach (var file in GetFilesFromSubfolder(dir, cwd, globs, excludeGlobs))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static string GetRelativeDirectoryPath(string parentDirectory, string directory)
        {
            var relativeDirectory = PathUtility.GetRelativePathToFile(parentDirectory, directory);
            return relativeDirectory.TrimEnd('\\', '/') + "/";
        }

        private static bool IsFileMatch(string path, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            return IsMatch(path, globs, excludeGlobs, false);
        }

        private static bool IsDirectoryMatch(string path, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            return IsMatch(path, globs, excludeGlobs, true);
        }

        private static bool IsMatch(string path, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs, bool partial)
        {
            foreach (var exclude in excludeGlobs)
            {
                if (exclude.Match(path, false))
                {
                    return false;
                }
            }
            foreach (var glob in globs)
            {
                if (glob.Match(path, partial))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
