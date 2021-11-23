// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using GlobExpressions;

namespace Microsoft.Docs.Build;

internal static class GlobUtility
{
    private static readonly char[] s_globChars = new char[] { '*', '?', '[', ']' };

    public static Func<string, bool> CreateGlobMatcher(string pattern)
    {
        var glob = CreateGlob(pattern);

        return path => !IsFileStartingWithDot(path) && glob(path);
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
                if (exclude != null && exclude(path))
                {
                    return false;
                }
            }

            foreach (var include in includeGlobs)
            {
                if (include != null && include(path))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static bool IsGlobString(string str)
        => str.IndexOfAny(s_globChars) >= 0;

    private static Func<string, bool> CreateGlob(string pattern)
    {
        pattern = PreProcessPattern(PathUtility.Normalize(pattern));

        if (KnownGlob.TryCreate(pattern) is KnownGlob knownGlob)
        {
            return knownGlob.IsMatch;
        }

        try
        {
            var options = PathUtility.IsCaseSensitive ? GlobOptions.None : GlobOptions.CaseInsensitive;

            return new Glob(pattern, options | GlobOptions.MatchFullPath | GlobOptions.Compiled).IsMatch;
        }
        catch (Exception ex)
        {
            Log.Write(ex);
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
        pattern = Regex.Replace(pattern, @"\*\*\/\*$", "**");
        return pattern.Replace("/**.", "/**/*.").Replace("/**/**/", "/**/");
    }

    /// <summary>
    /// Fast pass to process commonly known glob patterns:
    ///
    ///   - **/*.md
    ///   - **/includes/**
    ///   - **/includes/*.md
    ///   - **/includes/**/*.md
    ///   - includes/*.md
    ///   - includes/**/*.md
    ///   - includes/**
    /// </summary>
    private class KnownGlob
    {
        private readonly string? _extension;
        private readonly string? _startsWithFolder;
        private readonly string? _subFolder;
        private readonly bool _allowTrailingFolders;

        private KnownGlob(string? extension, string? startsWithFolder, string? subFolder, bool allowTrailingFolders)
        {
            _extension = extension;
            _startsWithFolder = startsWithFolder;
            _subFolder = subFolder;
            _allowTrailingFolders = allowTrailingFolders;
        }

        public static KnownGlob? TryCreate(string pattern)
        {
            var match = Regex.Match(pattern.Replace('\\', '/'), @"^(\*\*)?([\w-._/]+\/)?(\*\*\/?)?(\*\.\w+)?$");
            if (!match.Success)
            {
                return null;
            }

            var allowLeadingFolders = match.Groups[1].Success;
            var folder = match.Groups[2].Success ? match.Groups[2].Value : null;
            var allowTrailingFolders = match.Groups[3].Success;
            var extension = match.Groups[4].Success ? match.Groups[4].Value.TrimStart('*') : null;
            var startsWithFolder = !allowLeadingFolders ? folder : null;
            var subFolder = allowLeadingFolders ? folder?.TrimStart('/') : null;

            if (extension is null && folder is null)
            {
                return null;
            }

            if (allowLeadingFolders && folder is null)
            {
                allowTrailingFolders = true;
            }

            return new KnownGlob(extension, startsWithFolder, subFolder, allowTrailingFolders);
        }

        public bool IsMatch(string path)
        {
            // TODO: Use PathString as contract to remove this replace process
            path = path.Replace('\\', '/');

            var trailingFolderStartIndex = 0;

            // Handle file extension: *.md
            if (_extension != null && !path.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Handle path starts: includes/**
            if (_startsWithFolder != null)
            {
                if (!path.StartsWith(_startsWithFolder, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                trailingFolderStartIndex = _startsWithFolder.Length;
            }

            // Handle path in the middle: **/includes/**
            if (_subFolder != null)
            {
                var index = MatchSubFolder(path, _subFolder);
                if (index < 0)
                {
                    return false;
                }

                trailingFolderStartIndex = index + _subFolder.Length;
            }

            // Handle /**/ before extension
            if (!_allowTrailingFolders && path.AsSpan(trailingFolderStartIndex + 1).Contains('/'))
            {
                return false;
            }

            return true;
        }

        private static int MatchSubFolder(string path, string subFolder)
        {
            var searchStartIndex = path.Length;

            while (true)
            {
                var index = path.LastIndexOf(subFolder, searchStartIndex, StringComparison.OrdinalIgnoreCase);
                if (index > 0 && path[index - 1] != '/')
                {
                    searchStartIndex = index;
                    continue;
                }

                return index;
            }
        }
    }
}
