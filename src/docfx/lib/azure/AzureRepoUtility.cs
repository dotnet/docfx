// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class AzureRepoUtility
    {
        private static readonly Regex s_azureRepoUrlRegex =
            new Regex(
                @"^(https|http):\/\/(?<account>[^\/\s]+)\.visualstudio\.com\/(?<project>[^\/\s]+)\/_git\/(?<repository>[A-Za-z0-9_.-]+)((\/)?|(#(?<branch>\S+))?)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParse(string remote, out string project, out string repo)
        {
            project = repo = default;

            if (string.IsNullOrEmpty(remote))
                return false;

            var match = s_azureRepoUrlRegex.Match(remote);
            if (!match.Success)
                return false;

            project = match.Groups["project"].Value;
            repo = match.Groups["repository"].Value;

            return true;
        }
    }
}
