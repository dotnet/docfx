// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public static string GitHubUserCachePath => Path.Combine(CacheRoot, "github-users.json");

        public static string GetGitDir(string url)
        {
            return PathUtility.NormalizeFolder(Path.Combine(GitRoot, UrlToPath(url)));
        }

        public static string GetFileDownloadDir(string url)
        {
            // URL to a resource is case sensitive, query string matters, so hash the download path
            return PathUtility.NormalizeFolder(Path.Combine(DownloadsRoot, UrlToPath(url) + "-" + url.Trim().GetMd5HashShort()));
        }

        public static string GetCommitCachePath(string remote)
        {
            return Path.Combine(CacheRoot, "commits", HashUtility.GetMd5Hash(remote));
        }

        private static string UrlToPath(string url)
        {
            (url, _, _) = HrefUtility.SplitHref(url);

            // Trim https://
            var i = url.IndexOf(':');
            if (i > 0)
            {
                url = url.Substring(i);
            }

            return HrefUtility.EscapeUrl(url.TrimStart('/', '\\', '.', ':').Trim());
        }

        /// <summary>
        /// Get the global configuration path, default is under <see cref="s_root"/>
        /// </summary>
        private static string GetGlobalConfigPath()
        {
            var docfxGlobalConfig = Environment.GetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH");
            Config.TryGetConfigPath(s_root, out string configPath, out string configFile);
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
