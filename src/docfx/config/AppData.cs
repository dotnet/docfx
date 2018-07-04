// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class AppData
    {
        public static string RestoreDir => Path.Combine(s_appDataPath, ".docfx", "git");

        public static string LockDir => Path.Combine(s_appDataPath, ".docfx", "lock");

        public static string CacheDir => Path.Combine(s_appDataPath, ".docfx", "cache");

        private static readonly string s_appDataPath = GetAppDataPath();

        /// <summary>
        /// Get the restore root dir, default is the user proflie dir.
        /// User can set the DOCFX_APPDATA_PATH environment to change the root
        /// </summary>
        private static string GetAppDataPath()
        {
            // TODO: document this environment variable and show it in welcome message
            var docfxAppData = Environment.GetEnvironmentVariable("DOCFX_APPDATA_PATH");
            if (!string.IsNullOrEmpty(docfxAppData))
            {
                docfxAppData = Path.GetFullPath(docfxAppData);
            }

            return docfxAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
    }
}
