// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.HtmlToPdf;

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
