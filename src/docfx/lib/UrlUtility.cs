// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;

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
                @"^(https|http):\/\/(?<account>[^\/\s]+)\.visualstudio\.com\/(?<collection>[^\/\s]+\/)?(?<project>[^\/\s]+)\/_git\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            if (string.IsNullOrEmpty(targetPath))
                return targetUrl;

            var targetQueryParameters = HttpUtility.ParseQueryString(targetQuery.Length == 0 ? "" : targetQuery.Substring(1));
            var sourceQueryParameters = HttpUtility.ParseQueryString(sourceQuery);

            foreach (var key in sourceQueryParameters.AllKeys)
            {
                targetQueryParameters.Set(key, sourceQueryParameters[key]);
            }

            var query = targetQueryParameters.HasKeys() ? "?" + targetQueryParameters.ToString() : string.Empty;
            var fragment = (sourceFragment == null || sourceFragment.Length == 0) ? targetFragment : "#" + sourceFragment;

            return targetPath + query + fragment;
        }

        /// <summary>
        /// Get the git remote information from remote href
        /// </summary>
        /// <param name="remoteUrl">The git remote href like https://github.com/dotnet/docfx#master</param>
        public static (string remote, string refspec, bool hasRefSpec) SplitGitUrl(string remoteUrl)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteUrl));

            var (path, _, fragment) = SplitUrl(remoteUrl);

            path = path.TrimEnd('/', '\\');
            var hasRefSpec = !string.IsNullOrEmpty(fragment) && fragment.Length > 1;
            var refspec = hasRefSpec ? fragment.Substring(1) : "master";

            return (path, refspec, hasRefSpec);
        }

        public static DependencyType FragmentToDependencyType(string fragment)
        {
            Debug.Assert(string.IsNullOrEmpty(fragment) || fragment[0] == '#');

            return fragment != null && fragment.Length > 1 ? DependencyType.Bookmark : DependencyType.Link;
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

            // Uri.TryCreate does not handle some common errors like http:docs.com, so specialize them here
            if (char.IsLetter(ch) && link.Contains(':'))
            {
                return LinkType.External;
            }

            if (ch == '#')
            {
                return LinkType.SelfBookmark;
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
    }
}
