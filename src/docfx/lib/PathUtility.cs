// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide utils of path
    /// </summary>
    internal static class PathUtility
    {
        public static readonly bool IsCaseSensitive = GetIsCaseSensitive();

        public static readonly StringComparer PathComparer = IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        public static readonly StringComparison PathComparison = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        private static readonly HashSet<char> s_invalidPathChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct().ToHashSet();

        /// <summary>
        /// Check if the file is the same as matcher or is inside the directory specified by matcher.
        /// Both path should be normalized
        /// </summary>
        public static (bool match, bool isFileMatch, string remainingPath) Match(this string file, string matcher)
        {
            Debug.Assert(!file.EndsWith('/'));

            if (string.Equals(file, matcher, PathComparison))
            {
                return (true, true, file);
            }

            if (matcher == "./")
            {
                return (true, false, file);
            }

            if (!matcher.EndsWith('/'))
            {
                matcher += '/';
            }

            if (file.StartsWith(matcher, PathComparison))
            {
                return (true, false, Path.GetRelativePath(matcher, file).Replace('\\', '/'));
            }

            return default;
        }

        /// <summary>
        /// Finds a yaml or json file under the specified location
        /// </summary>
        public static string FindYamlOrJson(string pathWithoutExtension)
        {
            var fullPath = PathUtility.NormalizeFile(pathWithoutExtension + ".yml");
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            fullPath = PathUtility.NormalizeFile(pathWithoutExtension + ".json");
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            return null;
        }

        /// <summary>
        /// Create a relative path from one path to another file.
        /// Use this over <see cref="Path.GetRelativePath(string, string)"/> when
        /// <paramref name="fileRelativeTo"/> is a file.
        /// </summary>
        /// <param name="fileRelativeTo">The file path relative to</param>
        /// <param name="path">The original path </param>
        /// <returns>The relative path</returns>
        public static string GetRelativePathToFile(string fileRelativeTo, string path)
        {
            var directory = Path.GetDirectoryName(fileRelativeTo);
            if (string.IsNullOrEmpty(directory))
            {
                return path;
            }

            var result = Path.GetRelativePath(directory, path);

            // https://github.com/dotnet/corefx/issues/30263
            if (result == ".")
            {
                return Path.Combine("../", Path.GetFileName(path));
            }
            else if (result.EndsWith('\\') || result.EndsWith('/'))
            {
                return NormalizeFile(Path.Combine(result, Path.GetFileName(path)));
            }

            return result;
        }

        /// <summary>
        /// A normalized folder always ends with `/`, does not contain `\` and does not have consecutive `.` or `/`.
        /// </summary>
        /// <param name="path">The folder path want to be normalized</param>
        /// <returns>The normalized folder path</returns>
        public static string NormalizeFolder(string path)
        {
            var str = Normalize(path);
            if (str.Length == 0 || str == "/")
            {
                return "./";
            }

            if (!str.EndsWith('/'))
            {
                str += '/';
            }
            return str;
        }

        /// <summary>
        /// A normalized file cannot end with `/`, does not contain `\` and does not have consecutive `.` or `/`.
        /// </summary>
        /// <param name="path">The file path want to be normalized</param>
        /// <returns>The normalized file path</returns>
        public static string NormalizeFile(string path)
        {
            return Normalize(path);
        }

        /// <summary>
        /// Create a new directory from specified file path.
        /// </summary>
        /// <param name="filePath">The file path containing the directory to create</param>
        public static void CreateDirectoryFromFilePath(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            var directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath))
                return;
            Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Converts an URL to a human readable short name for directory or file
        /// </summary>
        public static string UrlToShortName(string url)
        {
            var hash = HashUtility.GetMd5HashShort(url);

            // Trim https://
            var index = url.IndexOf(':');
            if (index > 0)
            {
                url = url.Substring(index);
            }

            url = url.TrimStart('/', '\\', '.', ':').Trim();

            var result = new StringBuilder();

            // Take the surrounding 4 segments and the surrounding 8 chars in each segment, then remove invalid path chars.
            var segments = url.Split(new[] { '/', '\\', ' ', '?', '#' }, StringSplitOptions.RemoveEmptyEntries);
            for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                if (segmentIndex == 4 && segments.Length > 8)
                {
                    segmentIndex += segments.Length - 9;
                    continue;
                }

                var segment = segments[segmentIndex];
                for (var charIndex = 0; charIndex < segment.Length; charIndex++)
                {
                    var ch = segment[charIndex];
                    if (charIndex == 8 && segment.Length > 16)
                    {
                        result.Append("..");
                        charIndex += segment.Length - 17;
                        continue;
                    }
                    if (!s_invalidPathChars.Contains(ch))
                    {
                        result.Append(ch);
                    }
                }

                result.Append('+');
            }

            result.Append(hash);
            return result.ToString();
        }

        private static string Normalize(string path)
        {
            path = path.Replace('\\', '/');

            if (path.IndexOf('.') == -1 && !path.Contains("//") && !path.EndsWith('/'))
            {
                return path;
            }

            var parentCount = 0;
            var rooted = path[0] == '/';
            var stack = new List<string>();
            foreach (var segment in path.Split('/'))
            {
                if (segment == ".." && stack.Count > 0)
                    stack.RemoveAt(stack.Count - 1);
                else if (segment == "..")
                    parentCount++;
                else if (segment != "." && !string.IsNullOrEmpty(segment))
                    stack.Add(segment);
            }

            var res = new StringBuilder();
            if (rooted)
            {
                res.Append('/');
            }
            else
            {
                while (parentCount-- > 0)
                {
                    res.Append("../");
                }
            }

            var i = 0;
            foreach (var segment in stack)
            {
                if (segment.Length > 0)
                {
                    if (i++ > 0)
                    {
                        res.Append('/');
                    }
                    res.Append(segment);
                }
            }
            return res.ToString();
        }

        private static bool GetIsCaseSensitive()
        {
            // Fast pass for windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            // https://github.com/dotnet/corefx/blob/bffef76f6af208e2042a2f27bc081ee908bb390b/src/Common/src/System/IO/PathInternal.CaseSensitivity.cs#L37
            try
            {
                string pathWithUpperCase = Path.Combine(Path.GetTempPath(), "CASESENSITIVETEST" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
                using (new FileStream(pathWithUpperCase, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose))
                {
                    string lowerCased = pathWithUpperCase.ToLowerInvariant();
                    return !File.Exists(lowerCased);
                }
            }
            catch (Exception exc)
            {
                // In case something goes terribly wrong, we don't want to fail just because
                // of a casing test, so we assume case-insensitive-but-preserving.
                Debug.Fail("Casing test failed: " + exc);
                return false;
            }
        }
    }
}
