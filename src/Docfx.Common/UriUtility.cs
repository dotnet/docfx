// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;

namespace Docfx.Common;

public static class UriUtility
{
    private const char QueryMarker = '?';
    private const char FragmentMarker = '#';
    private static readonly char[] QueryAndFragmentMarkers = [QueryMarker, FragmentMarker];

    public static bool HasFragment(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        return uriString.Contains(FragmentMarker);
    }

    public static bool HasQueryString(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        return uriString.Contains(QueryMarker);
    }

    public static string GetFragment(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        var index = uriString.IndexOf(FragmentMarker);
        return index == -1 ? string.Empty : uriString.Substring(index);
    }

    public static string GetNonFragment(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        var index = uriString.IndexOf(FragmentMarker);
        return index == -1 ? uriString : uriString.Remove(index);
    }

    public static string GetQueryString(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        var index = uriString.IndexOf(QueryMarker);
        return index == -1 ? string.Empty : GetNonFragment(uriString.Substring(index));
    }

    public static string GetPath(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        var index = uriString.IndexOfAny(QueryAndFragmentMarkers);
        return index == -1 ? uriString : uriString.Remove(index);
    }

    public static string GetQueryStringAndFragment(string uriString)
    {
        ArgumentNullException.ThrowIfNull(uriString);

        var index = uriString.IndexOfAny(QueryAndFragmentMarkers);
        return index == -1 ? string.Empty : uriString.Substring(index);
    }

    /// <summary>
    /// merge two href into one
    /// path, query and fragment in <paramref name="source"/> will overwrite the one in <paramref name="target"/>
    /// </summary>
    public static string MergeHref(string target, string source)
    {
        var (targetPath, targetQueryString, targetFragment) = Split(target);
        var (sourcePath, sourceQueryString, sourceFragment) = Split(source);

        var targetParameters = HttpUtility.ParseQueryString(targetQueryString);
        var sourceParameters = HttpUtility.ParseQueryString(sourceQueryString);

        foreach (var key in sourceParameters.AllKeys)
        {
            targetParameters.Set(key, sourceParameters[key]);
        }

        var path = sourcePath?.Length == 0 ? targetPath : sourcePath;
        var query = targetParameters.HasKeys() ? QueryMarker + targetParameters.ToString() : string.Empty;
        var fragment = sourceFragment?.Length == 0 ? targetFragment : sourceFragment;

        return path + query + fragment;
    }

    public static (string path, string query, string fragment) Split(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (uri.Length == 0)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var (fragment, remaining) = SplitPart(uri, FragmentMarker);
        var (query, path) = SplitPart(remaining, QueryMarker);
        return (path, query, fragment);

        static (string result, string remaining) SplitPart(string partial, char marker)
        {
            var index = partial.IndexOf(marker);
            var result = index == -1 ? string.Empty : partial.Substring(index);
            var partRemaining = index == -1 ? partial : partial.Substring(0, index);
            return (result, partRemaining);
        }
    }
}
