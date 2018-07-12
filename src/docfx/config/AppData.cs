// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class AppData
    {
        public static readonly string AppDataDir = GetAppDataDir();

        public static string GitRestoreDir => Path.Combine(AppDataDir, "git");

        public static string UrlRestoreDir => Path.Combine(AppDataDir, "url");

        public static string RestoreLockDir => Path.Combine(AppDataDir, "restore-lock");

        public static string FileMutexDir => Path.Combine(AppDataDir, "file-mutex");

        public static string CacheDir => Path.Combine(AppDataDir, "cache");

        /// <summary>
        /// Get the restore root dir, default is the user proflie dir.
        /// User can set the DOCFX_APPDATA_PATH environment to change the root
        /// </summary>
        private static string GetAppDataDir()
        {
            // TODO: document this environment variable
            var docfxAppData = Environment.GetEnvironmentVariable("DOCFX_APPDATA_PATH");
            if (!string.IsNullOrEmpty(docfxAppData))
            {
                docfxAppData = Path.GetFullPath(docfxAppData);
            }

            return Path.Combine(!string.IsNullOrEmpty(docfxAppData) ? docfxAppData : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx");
        }
    }
}
