// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Docs
{
    internal static class PathUtil
    {
        /// <summary>
        /// Determines if a file or path is inside a folder. Input should both be normalized.
        /// </summary>
        public static bool Inside(string folder, string path)
        {
            Debug.Assert(folder.Length > 0 && folder[folder.Length - 1] == '/');

            return folder == "./" || path.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Create a relative path from one path to another file.
        /// Use this over <see cref="Path.GetRelativePath(string, string)"/> when
        /// <paramref name="fileRelativeTo"/> is a file.
        /// </summary>
        public static string GetRelativePathToFile(string fileRelativeTo, string path)
        {
            var directory = Path.GetDirectoryName(fileRelativeTo);
            if (string.IsNullOrEmpty(directory))
            {
                return path;
            }

            return Path.GetRelativePath(directory, path);
        }

        /// <summary>
        /// A normalized folder always ends with `/`, does not contain `\` and does not have consegative `.` or `/`.
        /// </summary>
        public static string NormalizeFolder(string path)
        {
            Debug.Assert(path.IndexOfAny(Path.GetInvalidPathChars()) < 0);

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
        /// A normalized file cannot end with `/`, does not contain `\` and does not have consegative `.` or `/`.
        /// </summary>
        public static string NormalizeFile(string path)
        {
            Debug.Assert(Path.GetDirectoryName(path).IndexOfAny(Path.GetInvalidPathChars()) < 0);
            Debug.Assert(Path.GetFileName(path).IndexOfAny(Path.GetInvalidFileNameChars()) < 0);

            return Normalize(path);
        }

        private static string Normalize(string path)
        {
            path = path.Replace('\\', '/');

            var needReorder = false;
            foreach (var c in path)
            {
                if (c == '.')
                {
                    needReorder = true;
                    break;
                }
            }
            if (!needReorder)
            {
                return path;
            }

            var parentCount = 0;
            var rooted = path[0] == '/';
            var stack = new List<string>();
            foreach (var segment in path.Split('/', '\\'))
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
