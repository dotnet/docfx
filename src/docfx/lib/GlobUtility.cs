// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class GlobUtility
    {
        public static Func<string, bool> CreateGlobMatcher(string[] includePatterns, string[] excludePatterns = null)
        {
            var ignoreCase = !PathUtility.IsCaseSensitive;
            var includeGlobs = Array.ConvertAll(includePatterns, CreateGlob);
            var excludeGlobs = excludePatterns != null ? Array.ConvertAll(excludePatterns, CreateGlob) : null;

            return IsGlobMatch;

            bool IsGlobMatch(string path)
            {
                // Ignore files starting with dot
                if (path.StartsWith('.') || path.Contains("/.") || path.Contains("\\."))
                {
                    return false;
                }

                path = path.Replace('\\', '/');

                if (ignoreCase)
                {
                    path = path.ToLowerInvariant();
                }

                foreach (var include in includeGlobs)
                {
                    if (include != null && include.IsMatch(path))
                    {
                        if (excludeGlobs != null)
                        {
                            foreach (var exclude in excludeGlobs)
                            {
                                if (exclude != null && exclude.IsMatch(path))
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
                return false;
            }

            Glob.Glob CreateGlob(string pattern)
            {
                try
                {
                    if (string.IsNullOrEmpty(pattern))
                    {
                        // https://github.com/kthompson/glob/issues/35
                        return null;
                    }
                    if (ignoreCase)
                    {
                        pattern = pattern.ToLowerInvariant();
                    }
                    return new Glob.Glob(pattern.Replace('\\', '/'), Glob.GlobOptions.Compiled);
                }
                catch (Exception ex)
                {
                    throw Errors.InvalidGlobPattern(pattern, ex).ToException(ex);
                }
            }
        }
    }
}
