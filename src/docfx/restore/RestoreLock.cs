// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class RestoreLock
    {
        private readonly RestoreItem _restoreItem;

        public RestoreLock(string docset)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            _restoreItem = Load(docset).Result;
        }

        public bool TryGetWorkTreeHead(string href, out string workTreeHead)
        {
            return _restoreItem.Git.TryGetValue(href, out workTreeHead);
        }

        public static Task Lock(string docset, Func<RestoreItem, RestoreItem> process)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var restoreLockFilePath = GetRestoreLockFilePath(docset);
            return ProcessUtility.ProcessLock(
                Path.GetRelativePath(AppData.RestoreLockDir, restoreLockFilePath),
                () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(restoreLockFilePath));
                    using (var fileStream = File.Open(restoreLockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        var sr = new StreamReader(fileStream);
                        var sw = new StreamWriter(fileStream);

                        // read restore item
                        var (_, restoreItem) = JsonUtility.Deserialize<RestoreItem>(sr.ReadToEnd());
                        if (restoreItem == null)
                            restoreItem = new RestoreItem();

                        // process restore item
                        restoreItem = process(restoreItem);

                        // write back restore item
                        fileStream.SetLength(0);
                        sw.Write(JsonUtility.Serialize(restoreItem));
                        sw.Flush();

                        return Task.CompletedTask;
                    }
                });
        }

        private static async Task<RestoreItem> Load(string docset)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var restoreLockFilePath = GetRestoreLockFilePath(docset);
            var restore = new RestoreItem();
            await ProcessUtility.ProcessLock(
                Path.GetRelativePath(AppData.RestoreLockDir, restoreLockFilePath),
                () =>
                {
                    if (File.Exists(restoreLockFilePath))
                    {
                        (_, restore) = JsonUtility.Deserialize<RestoreItem>(File.ReadAllText(restoreLockFilePath));
                    }

                    return Task.CompletedTask;
                });

            return restore;
        }

        private static string GetRestoreLockFilePath(string docset)
        {
            docset = PathUtility.NormalizeFile(Path.GetFullPath(docset));
            var docsetKey = Path.GetFileName(docset) + "-" + PathUtility.Encode(docset.GetMd5String());

            return Path.Combine(AppData.RestoreLockDir, $"{docsetKey}-lock.json");
        }
    }
}
