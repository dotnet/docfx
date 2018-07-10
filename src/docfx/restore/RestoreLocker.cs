// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class RestoreLocker
    {
        public static Task Lock(string docset, RestoreLock restoreLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var restoreLockFilePath = GetRestoreLockFilePath(docset);
            return ProcessUtility.ProcessLock(
                Path.GetRelativePath(AppData.RestoreLockDir, restoreLockFilePath),
                () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(restoreLockFilePath));
                    File.WriteAllText(restoreLockFilePath, JsonUtility.Serialize(restoreLock));
                    return Task.CompletedTask;
                });
        }

        public static async Task<RestoreLock> Load(string docset)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var restoreLockFilePath = GetRestoreLockFilePath(docset);
            var restore = new RestoreLock();
            await ProcessUtility.ProcessLock(
                Path.GetRelativePath(AppData.RestoreLockDir, restoreLockFilePath),
                () =>
                {
                    if (File.Exists(restoreLockFilePath))
                    {
                        restore = JsonUtility.Deserialize<RestoreLock>(File.ReadAllText(restoreLockFilePath)).Item2;
                    }

                    return Task.CompletedTask;
                });

            return restore;
        }

        private static string GetRestoreLockFilePath(string docset)
        {
            docset = PathUtility.NormalizeFile(Path.GetFullPath(docset));
            var docsetKey = Path.GetFileName(docset) + "-" + PathUtility.Encode(HashUtility.GetMd5String(docset));

            return Path.Combine(AppData.RestoreLockDir, $"{docsetKey}-lock.json");
        }
    }
}
