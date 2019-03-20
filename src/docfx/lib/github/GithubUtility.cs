// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class GitHubUtility
    {
        private static readonly Regex s_gitHubRepoUrlRegex =
           new Regex(
               @"^((https|http):\/\/github\.com)\/(?<account>[^\/\s]+)\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParse(string remote, out string owner, out string name)
        {
            owner = name = default;

            if (string.IsNullOrEmpty(remote))
                return false;

            var match = s_gitHubRepoUrlRegex.Match(remote);
            if (!match.Success)
            {
                return false;
            }

            owner = match.Groups["account"].Value;
            name = match.Groups["repository"].Value;

            return true;
        }
    }
}
