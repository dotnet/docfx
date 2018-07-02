// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide Utilities of File glob
    /// </summary>
    internal static class FileGlob
    {
        /// <summary>
        /// Get files with patterns
        /// </summary>
        public static IEnumerable<string> GetFiles(string cwd, string[] patterns, string[] excludePatterns)
        {
            if (!Directory.Exists(cwd))
            {
                return Array.Empty<string>();
            }

            var globList = patterns.Select(s => new GlobMatcher(s)).ToArray();
            var excludeGlobList = excludePatterns.Select(s => new GlobMatcher(s)).ToArray();

            return GetFilesCore(cwd, globList, excludeGlobList);
        }

        private static IEnumerable<string> GetFilesCore(string cwd, GlobMatcher[] globs, GlobMatcher[] excludeGlobs)
        {
            foreach (var file in GetFilesFromSubfolder(cwd, cwd, globs, excludeGlobs))
            {
                yield return Path.GetFullPath(file);
            }
        }

        private static IEnumerable<string> GetFilesFromSubfolder(string baseDirectory, string cwd, GlobMatcher[] globs, GlobMatcher[] excludeGlobs)
        {
            foreach (var file in Directory.EnumerateFiles(baseDirectory))
            {
                var relativePath = GetRelativeFilePath(cwd, file);
                if (IsMatch(relativePath, globs, excludeGlobs, directory: false))
                {
                    yield return file;
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(baseDirectory))
            {
                var relativePath = GetRelativeDirectoryPath(cwd, dir);
                if (IsMatch(relativePath, globs, excludeGlobs, directory: true))
                {
                    foreach (var file in GetFilesFromSubfolder(dir, cwd, globs, excludeGlobs))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static string GetRelativeFilePath(string directory, string file)
        {
            var subpath = file.Substring(directory.Length);

            // directory could be
            // 1. root folder, e.g. E:\ or /
            // 2. sub folder, e.g. a or a/ or a\
            return subpath.TrimStart('\\', '/');
        }

        private static string GetRelativeDirectoryPath(string parentDirectory, string directory)
        {
            var relativeDirectory = GetRelativeFilePath(parentDirectory, directory);
            return relativeDirectory.TrimEnd('\\', '/') + "/";
        }

        private static bool IsMatch(string path, GlobMatcher[] globs, GlobMatcher[] excludeGlobs, bool directory)
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
                if (glob.Match(path, directory))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
