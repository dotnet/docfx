// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.LearnValidation;

public static class Utility
{
    private static readonly Regex s_sshGitUrlRegex = new(@"git@(?<host>.+?):(?<userName>.+?)\/(?<repoName>.+)\.git", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Transform SSH URL to Https URL
    /// </summary>
    public static string TransformGitUrl(string repoUrl)
    {
        if (string.IsNullOrEmpty(repoUrl))
        {
            return repoUrl;
        }

        var match = s_sshGitUrlRegex.Match(repoUrl);
        if (match.Success)
        {
            var host = match.Groups["host"];
            var userName = match.Groups["userName"];
            var repoName = match.Groups["repoName"];
            return $"https://{host}/{userName}/{repoName}";
        }

        return repoUrl;
    }
}
