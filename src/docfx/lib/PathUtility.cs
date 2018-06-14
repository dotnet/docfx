// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide utils of path
    /// </summary>
    internal static class PathUtility
    {
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
        /// Encode file/folder name to a valide file/folder name
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The encoded file path</returns>
        public static string Encode(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            return Uri.EscapeDataString(path);
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

        private static string Normalize(string path)
        {
            path = path.Replace('\\', '/');

            if (path.IndexOf('.') == -1 && !path.Contains("//"))
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
    }
}
