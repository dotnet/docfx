// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class FileGlob
    {
        private readonly List<GlobMatcher> _includeGlobs;
        private readonly List<GlobMatcher> _excludeGlobs;

        public FileGlob(string[] patterns, string[] excludePatterns)
        {
            Debug.Assert(patterns != null);
            Debug.Assert(excludePatterns != null);

            var options = PathUtility.IsCaseSensitive ? GlobMatcher.DefaultCaseSensitiveOptions : GlobMatcher.DefaultOptions;
            _includeGlobs = patterns.Select(s => new GlobMatcher(s, options)).ToList();
            _excludeGlobs = excludePatterns.Select(s => new GlobMatcher(s, options)).ToList();
        }

        public IEnumerable<string> GetFiles(string cwd)
        {
            if (!Directory.Exists(cwd))
            {
                return Array.Empty<string>();
            }
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
        }

        public bool IsMatch(string relativePath)
        {
            foreach (var glob in _includeGlobs)
            {
                if (glob.Match(relativePath, false))
                {
                    foreach (var exclude in _excludeGlobs)
                    {
                        if (exclude.Match(relativePath, false))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<string> GetFiles(string cwd, string[] patterns, string[] excludePatterns)
        {
            var fileGlob = new FileGlob(patterns, excludePatterns);
            return fileGlob.GetFiles(cwd);
        }
    }
}
