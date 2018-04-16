// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs
{
    /// <summary>
    /// Provide functions for restoring dependency repositories
    /// </summary>
    internal static class Restore
    {
        private static readonly string s_restoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", ".git");

        /// <summary>
        /// Get git repo information from git remote href, like https://github.com/org/repo#master
        /// </summary>
        /// <param name="remote">The git remote href</param>
        /// <returns>The git repo information including local dir, git remote url and branch</returns>
        public static (string dir, string url, string rev) GetGitInfo(string remote)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var uri = new Uri(remote);
            var rev = (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment.Length <= 1) ? "master" : uri.Fragment.Substring(1);
            var url = uri.GetLeftPart(UriPartial.Path);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(s_restoreDir, repo);

            return (PathUtility.NormalizeFolder(dir), url, rev);
        }
    }
}
