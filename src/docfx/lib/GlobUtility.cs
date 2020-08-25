// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using GlobExpressions;

namespace Microsoft.Docs.Build
{
    internal static class GlobUtility
    {
        public static Func<string, bool> CreateGlobMatcher(string pattern)
        {
            var glob = CreateGlob(pattern);

            return path => !IsFileStartingWithDot(path) && glob.IsMatch(path);
        }

        public static Func<string, bool> CreateGlobMatcher(string[] includePatterns, string[]? excludePatterns = null)
        {
            var includeGlobs = Array.ConvertAll(includePatterns, CreateGlob);
            var excludeGlobs = Array.ConvertAll(excludePatterns ?? Array.Empty<string>(), CreateGlob);

            return IsMatch;

            bool IsMatch(string path)
            {
                if (IsFileStartingWithDot(path))
                {
                    return false;
                }

                foreach (var exclude in excludeGlobs)
                {
                    if (exclude != null && exclude.IsMatch(path))
                    {
                        return false;
                    }
                }

                foreach (var include in includeGlobs)
                {
                    if (include != null && include.IsMatch(path))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static Glob CreateGlob(string pattern)
        {
            try
            {
                var options = PathUtility.IsCaseSensitive ? GlobOptions.None : GlobOptions.CaseInsensitive;

                return new Glob(PreProcessPattern(pattern), options | GlobOptions.MatchFullPath | GlobOptions.Compiled);
            }
            catch (Exception ex)
            {
                throw Errors.Config.GlobPatternInvalid(pattern, ex).ToException(ex);
            }
        }

        private static bool IsFileStartingWithDot(string path)
        {
            return path.StartsWith('.') || path.Contains("/.") || path.Contains("\\.");
        }

        private static string PreProcessPattern(string pattern)
        {
            // Pre process glob pattern so `**.md` means `**/*.md`
            // **** => **, **.md => **/*.md
            pattern = Regex.Replace(pattern, @"\*{2,}", "**");
            pattern = Regex.Replace(pattern, @"^\*{2}\.", "**/*.");
            return pattern.Replace("/**.", "/**/*.");
        }
    }
}
