// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Microsoft.Docs.Build;

internal static class UrlUtility
{
    private static readonly Regex s_gitHubUrlRegex = new(
        @"^((https|http):\/\/github\.com)\/(?<account>[^\/\s]+)\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex s_azureReposUrlRegex = new(
        @"^(https|http):\/\/((?<account>[^\/\s]+)\.visualstudio\.com\/(?<collection>[^\/\s]+\/)?|dev\.azure\.com\/(?<account>[^\/\s]+)\/)+" +
        @"(?<project>[^\/\s]+)\/_git\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<char> s_invalidPathChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct().ToHashSet();

    public static string SanitizeUrl(string? url)
    {
        // For azure blob url, url without sas token should identify if the content has changed
        // https://learn.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1#how-a-shared-access-signature-works
        return Regex.Replace(url ?? "", @"^(https:\/\/.+?.blob.core.windows.net\/)(.*)\?(.*)$", match => $"{match.Groups[1]}{match.Groups[2]}");
    }

    /// <summary>
    /// Escapse the characters of URL path and keep the most of allowable characters unescaped.
    /// refer to https://www.rfc-editor.org/rfc/rfc3986.html#section-3.3
    /// refer to workitem: https://dev.azure.com/ceapex/Engineering/_workitems/edit/126389
    /// The following reserved characters are allowed in path:
    ///    %21 %24 %26  %27  %28 %29 %2A %2B  %2C  %3B %3D %3A %40
    ///    !   $   &    '    (   )   *   +    ,    ;   =   :   @
    /// </summary>
    /// <param name="urlPath">path of URL</param>
    /// <returns>escapsed path</returns>
    public static string EscapeUrlPath(string urlPath)
    {
        var segments = urlPath.Split(new char[] { '\\', '/' });
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = Uri.EscapeDataString(segments[i])
                .Replace("%21", "!")
                .Replace("%24", "$")
                .Replace("%26", "&")
                .Replace("%27", "'")
                .Replace("%28", "(")
                .Replace("%29", ")")
                .Replace("%2A", "*")
                .Replace("%2B", "+")
                .Replace("%2C", ",")
                .Replace("%3B", ";")
                .Replace("%3D", "=")
                .Replace("%3A", ":")
                .Replace("%40", "@");
        }
        return string.Join('/', segments);
    }

    /// <summary>
    /// Escapse the characters of URL query or fragment and keep the most of allowable characters unescaped.
    /// refer to https://www.rfc-editor.org/rfc/rfc3986.html#section-3.4
    /// The following reserved characters are allowed in query:
    ///    %21 %24 %26  %27  %28 %29 %2A %2B  %2C  %3B %3D %3A %40 %2F %3F
    ///    !   $   &    '    (   )   *   +    ,    ;   =   :   @   /   ?
    /// </summary>
    /// <param name="urlQuery">query or fragment of URL without ? and #</param>
    /// <returns>escapsed path</returns>
    public static string EscapeUrlQueryOrFragment(string urlQuery)
    {
        return Uri.EscapeDataString(urlQuery)
                .Replace("%21", "!")
                .Replace("%24", "$")
                .Replace("%26", "&")
                .Replace("%27", "'")
                .Replace("%28", "(")
                .Replace("%29", ")")
                .Replace("%2A", "*")
                .Replace("%2B", "+")
                .Replace("%2C", ",")
                .Replace("%3B", ";")
                .Replace("%3D", "=")
                .Replace("%3A", ":")
                .Replace("%40", "@")
                .Replace("%2F", "/")
                .Replace("%3F", "?");
    }

    /// <summary>
    /// Split href to path, fragment and query
    /// </summary>
    /// <param name="url">The href</param>
    /// <returns>The splitted path, query and fragment</returns>
    public static (string path, string query, string fragment) SplitUrl(string url)
    {
        string path;
        var query = "";
        var fragment = "";

        var fragmentIndex = url.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            fragment = url[fragmentIndex..];
            var queryIndex = url.IndexOf('?', 0, fragmentIndex);
            if (queryIndex >= 0)
            {
                query = url[queryIndex..fragmentIndex];
                path = url[..queryIndex];
            }
            else
            {
                path = url[..fragmentIndex];
            }
        }
        else
        {
            var queryIndex = url.IndexOf('?');
            if (queryIndex >= 0)
            {
                query = url[queryIndex..];
                path = url[..queryIndex];
            }
            else
            {
                path = url;
            }
        }

        return (path, query, fragment);
    }

    /// <summary>
    /// Combines URL segments into a single URL.
    /// </summary>
    public static string Combine(params string[] urlSegments)
    {
        return Path.Combine(urlSegments).Replace('\\', '/');
    }

    /// <summary>
    /// <paramref name="sourceQuery"/> and <paramref name="sourceFragment"/> will overwrite the ones in <paramref name="targetUrl"/>
    /// </summary>
    public static string MergeUrl(string targetUrl, string? sourceQuery, string? sourceFragment = null)
    {
        var (targetPath, targetQuery, targetFragment) = SplitUrl(targetUrl);

        var targetQueryParameters = HttpUtility.ParseQueryString(targetQuery);

        var sourceQueryParameters = sourceQuery is null ? null : HttpUtility.ParseQueryString(sourceQuery);
        if (sourceQueryParameters != null)
        {
            foreach (var key in sourceQueryParameters.AllKeys)
            {
                targetQueryParameters[key] = sourceQueryParameters[key];
            }
        }

        var query = targetQueryParameters.Count > 0 ? targetQueryParameters.ToQueryString() : "";
        if (string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(sourceQuery))
        {
            query = sourceQuery;
        }

        var fragment = (sourceFragment == null || sourceFragment.Length == 0)
            ? (!string.IsNullOrEmpty(targetFragment) ? targetFragment : "")
            : (!string.IsNullOrEmpty(sourceFragment) ? sourceFragment : "");

        return targetPath + query + fragment;
    }

    public static string GetRelativeUrl(string relativeToUrl, string url)
    {
        if (!relativeToUrl.StartsWith('/'))
        {
            throw new ArgumentException("", nameof(relativeToUrl));
        }

        if (!url.StartsWith('/'))
        {
            return url;
        }

        var (relativeToPath, _, _) = SplitUrl(relativeToUrl);
        var (path, query, fragment) = SplitUrl(url);

        // Find the last common segment
        var i = 0;
        var segmentIndex = 0;
        while (i < path.Length && i < relativeToPath.Length)
        {
            var ch = path[i];
            if (ch != relativeToPath[i])
            {
                break;
            }

            i++;
            if (ch == '/')
            {
                segmentIndex = i;
            }
        }

        // Count remaining segments in relativeToUrl
        var remainingSegmentCount = 0;
        for (i = segmentIndex; i < relativeToPath.Length; i++)
        {
            if (relativeToPath[i] == '/')
            {
                remainingSegmentCount++;
            }
        }

        // Build result
        var result = new StringBuilder(path.Length);

        for (i = 0; i < remainingSegmentCount; i++)
        {
            result.Append("../");
        }

        if (segmentIndex >= path.Length)
        {
            if (remainingSegmentCount == 0)
            {
                result.Append("./");
            }
        }
        else
        {
            result.Append(path, segmentIndex, path.Length - segmentIndex);
        }

        result.Append(query);
        result.Append(fragment);
        return result.ToString();
    }

    public static LinkType GetLinkType(string? link)
    {
        if (string.IsNullOrEmpty(link))
        {
            return LinkType.RelativePath;
        }

        if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            if (string.IsNullOrEmpty(uri.DnsSafeHost) && uri.Scheme == Uri.UriSchemeFile)
            {
                return (uri.AbsolutePath.StartsWith('/') || uri.AbsolutePath.StartsWith('\\'))
                    ? LinkType.AbsolutePath
                    : LinkType.WindowsAbsolutePath;
            }
            return LinkType.External;
        }

        return link[0] switch
        {
            '/' or '\\' => LinkType.AbsolutePath,
            '#' => LinkType.SelfBookmark,
            _ => LinkType.RelativePath,
        };
    }

    public static bool IsHttp(string str)
    {
        return !string.IsNullOrEmpty(str)
            && Uri.TryCreate(str, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Converts an URL to a human readable short name for directory or file
    /// </summary>
    public static string UrlToShortName(string url)
    {
        url = SanitizeUrl(url);

        var hash = HashUtility.GetSha256HashShort(url);

        // Trim https://
        var index = url.IndexOf(':');
        if (index > 0)
        {
            url = url[index..];
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

    public static bool TryParseGitHubUrl(
        string? remoteUrl, [NotNullWhen(true)] out string? owner, [NotNullWhen(true)] out string? name)
    {
        owner = name = default;
        if (string.IsNullOrEmpty(remoteUrl))
        {
            return false;
        }

        var match = s_gitHubUrlRegex.Match(remoteUrl);
        if (!match.Success)
        {
            return false;
        }

        owner = match.Groups["account"].Value;
        name = match.Groups["repository"].Value;

        return true;
    }

    public static bool TryParseAzureReposUrl(
        string remoteUrl, [NotNullWhen(true)] out string? project, [NotNullWhen(true)] out string? repo, [NotNullWhen(true)] out string? org)
    {
        project = repo = org = default;

        if (string.IsNullOrEmpty(remoteUrl))
        {
            return false;
        }

        var match = s_azureReposUrlRegex.Match(remoteUrl);
        if (!match.Success)
        {
            return false;
        }

        org = match.Groups["account"].Value;
        project = match.Groups["project"].Value;
        repo = match.Groups["repository"].Value;

        return true;
    }

    public static string RemoveLeadingHostName(string url, string hostName, bool removeLocale = false)
    {
        if (string.IsNullOrEmpty(hostName))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        if (!string.Equals(uri.Host, hostName, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var path = MergeUrl(uri.PathAndQuery, "", uri.Fragment).TrimStart('/');
        if (!removeLocale)
        {
            return $"/{path}";
        }

        var slashIndex = path.IndexOf('/');
        if (slashIndex < 0)
        {
            return $"/{path}";
        }

        var firstSegment = path[..slashIndex];
        return LocalizationUtility.IsValidLocale(firstSegment)
            ? $"{path[firstSegment.Length..]}"
            : $"/{path}";
    }

    public static string GetBookmark(string uid)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < uid.Length; i++)
        {
            var ch = char.ToLowerInvariant(uid[i]);
            switch (ch)
            {
                case '"' or '\'' or '%' or '^' or '\\':
                    continue;
                case '<' or '[':
                    sb.Append('(');
                    break;
                case '>' or ']':
                    sb.Append(')');
                    break;
                case '{':
                    sb.Append("((");
                    break;
                case '}':
                    sb.Append("))");
                    break;
                case char c when (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'):
                case '(' or ')' or '*' or '@':
                    sb.Append(ch);
                    break;
                default:
                    if (sb.Length == 0 || sb[^1] == '-')
                    {
                        continue;
                    }
                    sb.Append('-');
                    break;
            }
        }

        if (sb[^1] == '-')
        {
            for (var i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == '-')
                {
                    sb.Remove(i, 1);
                }
                else
                {
                    break;
                }
            }
        }

        return sb.ToString();
    }

    private static string ToQueryString(this NameValueCollection collection)
    {
        var result = new StringBuilder("?");
        foreach (var key in collection.AllKeys)
        {
            if (string.IsNullOrEmpty(key))
            {
                result.Append($"{collection[key]}&");
            }
            else
            {
                result.Append($"{key}={collection[key]}&");
            }
        }
        result.Remove(result.Length - 1, 1);
        return result.ToString();
    }
}
