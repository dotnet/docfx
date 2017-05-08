// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.IO;

    public static class PdfHelper
    {
        public static string RemoveUrlQueryString(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }
            var queryStringIndex = url.IndexOf("?", StringComparison.OrdinalIgnoreCase);
            if (queryStringIndex != -1)
            {
                url = url.Substring(0, queryStringIndex);
            }
            return url;
        }

        public static string RemoveUrlBookmark(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }
            var bookmarkIndex = url.LastIndexOf("#", StringComparison.OrdinalIgnoreCase);
            if (bookmarkIndex != -1)
            {
                url = url.Substring(0, bookmarkIndex);
            }

            return url;
        }

        public static string TrimStartPath(this string path)
        {
            return path.TrimStart('/').TrimStart('\\');
        }

        public static string NormalizeFileLocalPath(string basePath, string relativePath, bool toLower = true)
        {
            string localPath = new Uri(Path.Combine(basePath, relativePath.TrimStartPath())).LocalPath;
            return toLower ? localPath.ToLower() : localPath;
        }
    }
}
