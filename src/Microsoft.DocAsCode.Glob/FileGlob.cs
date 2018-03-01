// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class FileGlob
    {
        public static IEnumerable<string> GetFiles(string cwd, IEnumerable<string> patterns, IEnumerable<string> excludePatterns, GlobMatcherOptions options = GlobMatcher.DefaultOptions)
        {
            // If there is no pattern, nothing will be included
            if (patterns == null) return Enumerable.Empty<string>();
            if (string.IsNullOrEmpty(cwd)) cwd = Directory.GetCurrentDirectory();

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
                yield return NormalizeToFullPath(file);
            }
        }

        private static IEnumerable<string> GetFilesFromSubfolder(string baseDirectory, string cwd, IEnumerable<GlobMatcher> globs, IEnumerable<GlobMatcher> excludeGlobs)
        {
            foreach (var file in Directory.GetFiles(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = GetRelativeFilePath(cwd, file);
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

        private static string NormalizeToFullPath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
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
                if (exclude.Match(path, false)) return false;
            }
            foreach (var glob in globs)
            {
                if (glob.Match(path, partial)) return true;
            }
            return false;
        }
    }
}