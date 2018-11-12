// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class AppData
    {
        public static readonly string AppDataDir = GetAppDataDir();

        public static string GitDir => Path.Combine(AppDataDir, "git");

        public static string DownloadsDir => Path.Combine(AppDataDir, "downloads");

        public static string MutexDir => Path.Combine(AppDataDir, "mutex");

        public static string CacheDir => Path.Combine(AppDataDir, "cache");

        public static string GlobalConfigPath => GetGlobalConfigPath();

        public static string GetGitDir(string url)
        {
            return PathUtility.NormalizeFolder(Path.Combine(GitDir, UrlToPath(url)));
        }

        public static string GetFileDownloadDir(string url)
        {
            // URL to a resource is case sensitive, query string matters, so the download path needs hash
            return PathUtility.NormalizeFolder(Path.Combine(DownloadsDir, UrlToPath(url) + "-" + url.Trim().GetMd5HashShort()));
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
        /// Get the global configuration path, default is under <see cref="AppDataDir"/>
        /// </summary>
        private static string GetGlobalConfigPath()
        {
            var docfxGlobalConfig = Environment.GetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH");
            Config.TryGetConfigPath(AppDataDir, out string configPath, out string configFile);
            return string.IsNullOrEmpty(docfxGlobalConfig) ? configPath : Path.GetFullPath(docfxGlobalConfig);
        }

        /// <summary>
        /// Get the application cache root dir, default is under user proflie dir.
        /// User can set the DOCFX_APPDATA_PATH environment to change the root
        /// </summary>
        private static string GetAppDataDir()
        {
            var docfxAppData = Environment.GetEnvironmentVariable("DOCFX_APPDATA_PATH");

            return string.IsNullOrEmpty(docfxAppData)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx")
                : Path.GetFullPath(docfxAppData);
        }
    }
}
