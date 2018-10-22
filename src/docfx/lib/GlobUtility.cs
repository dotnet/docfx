// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class GlobUtility
    {
        public static Func<string, bool> CreateGlobMatcher(string[] includePatterns, string[] excludePatterns)
        {
            var includeGlobs = Array.ConvertAll(includePatterns, CreateGlob);
            var excludeGlobs = Array.ConvertAll(excludePatterns, CreateGlob);

            return IsGlobMatch;

            bool IsGlobMatch(string path)
            {
                // Ignore files starting with dot
                if (path.StartsWith('.') || path.Contains("/.") || path.Contains("\\."))
                {
                    return false;
                }

                path = path.Replace('\\', '/');

                foreach (var include in includeGlobs)
                {
                    if (include.IsMatch(path))
                    {
                        foreach (var exclude in excludeGlobs)
                        {
                            if (exclude.IsMatch(path))
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

        private static Glob.Glob CreateGlob(string pattern)
        {
            try
            {
                return new Glob.Glob(pattern.Replace('\\', '/'), Glob.GlobOptions.Compiled);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidGlob(pattern, ex).ToException(ex);
            }
        }
    }
}
