// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class AppData
    {
        public static string Root => TestQuirks.AppDataPath?.Invoke() ?? GetAppDataRoot();

        public static string GitRoot => Path.Combine(Root, "git6");

        public static string DownloadsRoot => Path.Combine(Root, "downloads2");

        public static string MutexRoot => Path.Combine(Root, "mutex");

        public static string CacheRoot => EnvironmentVariable.CachePath ?? Path.Combine(Root, "cache");

        public static string StateRoot => EnvironmentVariable.StatePath ?? Path.Combine(Root, "state");

        public static string GitHubUserCachePath => Path.Combine(CacheRoot, "github-users.json");

        public static string MicrosoftGraphCachePath => Path.Combine(CacheRoot, "msgraph-users.json");

        public static string GetFileDownloadDir(string url)
        {
            return Path.Combine(DownloadsRoot, PathUtility.UrlToShortName(url));
        }

        public static string GetCommitCachePath(string remote)
        {
            return Path.Combine(CacheRoot, "commits", HashUtility.GetMd5Hash(remote));
        }

        public static string GetCommitBuildTimePath(string remote, string branch)
        {
            return Path.Combine(
                StateRoot, "history", $"build_history_{HashUtility.GetMd5Guid(remote)}_{HashUtility.GetMd5Guid(branch)}.json");
        }

        /// <summary>
        /// Get the global configuration path, default is under <see cref="Root"/>
        /// </summary>
        public static bool TryGetGlobalConfigPath([NotNullWhen(true)] out string? path)
        {
            if (EnvironmentVariable.GlobalConfigPath != null && File.Exists(EnvironmentVariable.GlobalConfigPath))
            {
                path = EnvironmentVariable.GlobalConfigPath;
                return true;
            }

            path = PathUtility.FindYamlOrJson(Root, "docfx");
            return path != null;
        }

        /// <summary>
        /// Get the application cache root dir, default is under user proflie dir.
        /// User can set the DOCFX_APPDATA_PATH environment to change the root
        /// </summary>
        private static string GetAppDataRoot()
        {
            return EnvironmentVariable.AppDataPath != null
                ? Path.GetFullPath(EnvironmentVariable.AppDataPath)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx");
        }
    }
}
