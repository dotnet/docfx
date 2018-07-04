// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class FileGlob
    {
        public static IEnumerable<string> GetFiles(string cwd, string[] patterns, string[] excludePatterns)
        {
            if (!Directory.Exists(cwd))
            {
                return Array.Empty<string>();
            }

            var options = PathUtility.IsCaseSensitive ? GlobMatcher.DefaultCaseSensitiveOptions : GlobMatcher.DefaultOptions;
            var includeGlobs = patterns.Select(s => new GlobMatcher(s, options)).ToList();
            var excludeGlobs = excludePatterns.Select(s => new GlobMatcher(s, options)).ToList();
            var result = new ConcurrentBag<string>();
            Parallel.ForEach(Directory.EnumerateFiles(cwd, "*.*", SearchOption.AllDirectories), MatchFile);
            return result;

            void MatchFile(string file)
            {
                // cwd could be
                // 1. root folder, e.g. E:\ or /
                // 2. sub folder, e.g. a or a/ or a\
                var relativePath = file.Substring(cwd.Length).TrimStart('\\', '/');
                if (IsMatch(relativePath))
                {
                    result.Add(Path.GetFullPath(file));
                }
            }

            bool IsMatch(string path)
            {
                foreach (var glob in includeGlobs)
                {
                    if (glob.Match(path, false))
                    {
                        foreach (var exclude in excludeGlobs)
                        {
                            if (exclude.Match(path, false))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
