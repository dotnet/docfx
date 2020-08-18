// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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

        private static readonly EnumerationOptions s_enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };
        private static readonly HashSet<char> s_invalidPathChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct().ToHashSet();

        /// <summary>
        /// Finds a yaml or json file under the specified location
        /// </summary>
        public static T? LoadYamlOrJson<T>(ErrorBuilder errors, string directory, string fileNameWithoutExtension) where T : class, new()
        {
            var fileName = fileNameWithoutExtension + ".yml";
            var fullPath = Path.Combine(directory, fileName);
            if (File.Exists(fullPath))
            {
                return YamlUtility.Deserialize<T>(errors, File.ReadAllText(fullPath), new FilePath(fileName));
            }

            fileName = fileNameWithoutExtension + ".json";
            fullPath = Path.Combine(directory, fileName);
            if (File.Exists(fullPath))
            {
                return JsonUtility.Deserialize<T>(errors, File.ReadAllText(fullPath), new FilePath(fileName));
            }

            return null;
        }

        /// <summary>
        /// Enumerates files inside a directory, returns path relative to <paramref name="directory"/>.
        /// </summary>
        public static IEnumerable<PathString> GetFiles(string directory)
        {
            return new FileSystemEnumerable<PathString>(directory, ToPathString, s_enumerationOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName[0] != '.',
                ShouldRecursePredicate =
                 (ref FileSystemEntry entry) => entry.FileName[0] != '.' && !entry.FileName.Equals("_site", StringComparison.OrdinalIgnoreCase),
            };

            static PathString ToPathString(ref FileSystemEntry entry)
            {
                Debug.Assert(!entry.IsDirectory);

                var path = entry.RootDirectory.Length == entry.Directory.Length
                    ? entry.FileName.ToString()
                    : string.Concat(entry.Directory.Slice(entry.RootDirectory.Length + 1), "/", entry.FileName);

                return PathString.DangerousCreate(path);
            }
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
            path = Normalize(path);
            return path.EndsWith('/') ? path[0..^1] : path;
        }

        /// <summary>
        /// Normalize '\', './', '..' in given path string.
        /// </summary>
        /// <param name="path">The path want to be normalized</param>
        /// <returns>The normalized path</returns>
        public static string Normalize(string path)
        {
            path = path.Replace('\\', '/');

            if (!path.Contains('.') && !path.Contains("//"))
            {
                return path;
            }

            var parentCount = 0;
            var rooted = path[0] == '/';
            var stack = new List<string>();
            foreach (var segment in path.Split('/'))
            {
                if (segment == ".." && stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (segment == "..")
                {
                    parentCount++;
                }
                else if (segment != "." && !string.IsNullOrEmpty(segment))
                {
                    stack.Add(segment);
                }
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

            if (path[^1] == '/' && res.Length > 0 && res[res.Length - 1] != '/')
            {
                res.Append('/');
            }

            return res.ToString();
        }

        /// <summary>
        /// Converts an URL to a human readable short name for directory or file
        /// </summary>
        public static string UrlToShortName(string url)
        {
            url = RemoveQueryForBlobUrl(url);

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

        // For azure blob url, url without sas token should identify if the content has changed
        // https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1#how-a-shared-access-signature-works
        private static string RemoveQueryForBlobUrl(string url)
        {
            return Regex.Replace(url, @"^(https:\/\/.+?.blob.core.windows.net\/)(.*)\?(.*)$", match => $"{match.Groups[1]}{match.Groups[2]}");
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
                var pathWithUpperCase = Path.Combine(Path.GetTempPath(), "CASESENSITIVETEST" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
                using (new FileStream(pathWithUpperCase, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose))
                {
                    var lowerCased = pathWithUpperCase.ToLowerInvariant();
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
