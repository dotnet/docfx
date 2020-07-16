﻿using System.Text.RegularExpressions;

namespace TripleCrownValidation
{
    public static class Utility
    {
        private static Regex s_sshGitUrlRegex = new Regex(@"git@(?<host>.+?):(?<userName>.+?)\/(?<repoName>.+)\.git", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Transform SSH URL to https URL
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
}
