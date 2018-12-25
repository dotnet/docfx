// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class AppData
    {
        private static readonly string s_root = GetAppDataRoot();

        public static string GitRoot => Path.Combine(s_root, "git");

        public static string DownloadsRoot => Path.Combine(s_root, "downloads");

        public static string MutexRoot => Path.Combine(s_root, "mutex");

        public static string CacheRoot => Path.Combine(s_root, "cache");

        public static string GlobalConfigPath => GetGlobalConfigPath();

        public static string DefaultGitHubUserCachePath => Path.Combine(CacheRoot, "github-users.json");

        public static string GetGitDir(string remote)
        {
            Debug.Assert(!remote.Contains('#'));
            return PathUtility.NormalizeFolder(Path.Combine(GitRoot, PathUtility.UrlToShortName(remote)));
        }

        public static string GetFileDownloadDir(string url)
        {
            return PathUtility.NormalizeFolder(Path.Combine(DownloadsRoot, PathUtility.UrlToShortName(url)));
        }

        public static string GetCommitCachePath(string remote)
        {
            return Path.Combine(CacheRoot, "commits", HashUtility.GetMd5Hash(remote));
        }

        public static string GetGitHubUserCachePath(string url)
        {
            return Path.Combine(CacheRoot, "github-users", PathUtility.UrlToShortName(url));
        }

        /// <summary>
        /// Get the global configuration path, default is under <see cref="s_root"/>
        /// </summary>
        private static string GetGlobalConfigPath()
        {
            var docfxGlobalConfig = Environment.GetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH");
            var configPath = PathUtility.FindYamlOrJson(Path.Combine(s_root, "docfx"));
            return string.IsNullOrEmpty(docfxGlobalConfig) ? configPath : Path.GetFullPath(docfxGlobalConfig);
        }

        /// <summary>
        /// Get the application cache root dir, default is under user proflie dir.
        /// User can set the DOCFX_APPDATA_PATH environment to change the root
        /// </summary>
        private static string GetAppDataRoot()
        {
            var docfxAppData = Environment.GetEnvironmentVariable("DOCFX_APPDATA_PATH");

            return string.IsNullOrEmpty(docfxAppData)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx")
                : Path.GetFullPath(docfxAppData);
        }
    }
}
