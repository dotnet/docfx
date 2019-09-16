// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class UrlUtility
    {
        private static readonly Regex s_gitHubUrlRegex =
           new Regex(
               @"^((https|http):\/\/github\.com)\/(?<account>[^\/\s]+)\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex s_azureReposUrlRegex =
            new Regex(
                @"^(https|http):\/\/((?<account1>[^\/\s]+)\.visualstudio\.com\/(?<collection>[^\/\s]+\/)?|dev\.azure\.com\/(?<account2>[^\/\s]+)\/)+(?<project>[^\/\s]+)\/_git\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly char[] s_queryFragmentLeadingChars = new char[] { '#', '?' };

        /// <summary>
        /// Split href to path, fragement and query
        /// </summary>
        /// <param name="url">The href</param>
        /// <returns>The splited path, query and fragment</returns>
        public static (string path, string query, string fragment) SplitUrl(string url)
        {
            var path = "";
            var query = "";
            var fragment = "";

            var fragmentIndex = url.IndexOf('#');
            if (fragmentIndex >= 0)
            {
                fragment = url.Substring(fragmentIndex);
                var queryIndex = url.IndexOf('?', 0, fragmentIndex);
                if (queryIndex >= 0)
                {
                    query = url.Substring(queryIndex, fragmentIndex - queryIndex);
                    path = url.Substring(0, queryIndex);
                }
                else
                {
                    path = url.Substring(0, fragmentIndex);
                }
            }
            else
            {
                var queryIndex = url.IndexOf('?');
                if (queryIndex >= 0)
                {
                    query = url.Substring(queryIndex);
                    path = url.Substring(0, queryIndex);
                }
                else
                {
                    path = url;
                }
            }

            return (path, query, fragment);
        }

        /// <summary>
        /// <paramref name="sourceQuery"/> and <paramref name="sourceFragment"/> will overwrite the ones in <paramref name="targetUrl"/>
        /// </summary>
        public static string MergeUrl(string targetUrl, string sourceQuery, string sourceFragment)
        {
            var (targetPath, targetQuery, targetFragment) = SplitUrl(targetUrl);

            var targetQueryParameters = ParseQueryString(targetQuery);
            var sourceQueryParameters = ParseQueryString(sourceQuery);

            foreach (var key in sourceQueryParameters.Keys)
            {
                targetQueryParameters[key] = sourceQueryParameters[key];
            }

            var query = targetQueryParameters.Any() ?
                $"?{string.Join("&", targetQueryParameters.Select(x => !string.IsNullOrEmpty(x.Value) ? $"{x.Key}={x.Value}" : x.Key))}"
                : string.Empty;
            var fragment = (sourceFragment == null || sourceFragment.Length == 0)
                ? (!string.IsNullOrEmpty(targetFragment) ? targetFragment : "")
                : (!string.IsNullOrEmpty(sourceFragment) ? sourceFragment : "");

            return targetPath + query + fragment;
        }

        public static DependencyType FragmentToDependencyType(string fragment)
        {
            return fragment != null && fragment.Length > 1 ? DependencyType.Bookmark : DependencyType.Link;
        }

        public static string GetRelativeUrl(string relativeToUrl, string url)
        {
            if (!relativeToUrl.StartsWith('/'))
            {
                throw new ArgumentException("", nameof(relativeToUrl));
            }

            if (url == null || !url.StartsWith('/'))
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

            if (!string.IsNullOrEmpty(query))
            {
                result.Append(query);
            }
            if (!string.IsNullOrEmpty(fragment))
            {
                result.Append(fragment);
            }
            return result.ToString();
        }

        public static LinkType GetLinkType(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return LinkType.RelativePath;
            }

            var ch = link[0];

            if (ch == '/' || ch == '\\')
            {
                if (link.Length > 1 && (link[1] == '/' || link[1] == '\\'))
                {
                    return LinkType.External;
                }
                return LinkType.AbsolutePath;
            }

            // If it is a windows rooted path like C:
            if (link.Length > 2 && link[1] == ':')
            {
                return LinkType.WindowsAbsolutePath;
            }

            if (Uri.TryCreate(link, UriKind.Absolute, out _))
            {
                return LinkType.External;
            }

            if (ch == '#')
            {
                return LinkType.SelfBookmark;
            }

            // Uri.TryCreate does not handle some common errors like http:docs.com, so specialize them here
            if (char.IsLetter(ch))
            {
                var colonIndex = link.IndexOf(':');
                if (colonIndex > 0
                    && link.IndexOfAny(s_queryFragmentLeadingChars) is var queryOrFragmentIndex
                    && (queryOrFragmentIndex < 0 || colonIndex < queryOrFragmentIndex))
                    return LinkType.External;
            }

            return LinkType.RelativePath;
        }

        public static bool IsHttp(string str)
        {
            return !string.IsNullOrEmpty(str)
                && Uri.TryCreate(str, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool TryParseGitHubUrl(string remoteUrl, out string owner, out string name)
        {
            owner = name = default;

            if (string.IsNullOrEmpty(remoteUrl))
                return false;

            var match = s_gitHubUrlRegex.Match(remoteUrl);
            if (!match.Success)
            {
                return false;
            }

            owner = match.Groups["account"].Value;
            name = match.Groups["repository"].Value;

            return true;
        }

        public static bool TryParseAzureReposUrl(string remoteUrl, out string project, out string repo)
        {
            project = repo = default;

            if (string.IsNullOrEmpty(remoteUrl))
                return false;

            var match = s_azureReposUrlRegex.Match(remoteUrl);
            if (!match.Success)
                return false;

            project = match.Groups["project"].Value;
            repo = match.Groups["repository"].Value;

            return true;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            Debug.Assert(string.IsNullOrEmpty(query) || query[0] == '?');
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query) || query == "?")
            {
                return result;
            }

            foreach (var item in query.Substring(1).Split('&'))
            {
                var index = item.IndexOf('=');
                if (index != -1)
                {
                    var key = item.Substring(0, index);
                    var value = item.Substring(index + 1);
                    result.Add(key, value);
                }
                else
                {
                    result.Add(item, null);
                }
            }
            return result;
        }
    }
}
