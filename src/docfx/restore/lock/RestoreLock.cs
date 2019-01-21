// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreLock
    {
        public static async Task<DependencyLock> Load(string docset, string dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            var (_, restoredLockFile) = RestoreMap.GetFileRestorePath(docset, dependencyLockPath);

            var content = await ProcessUtility.ReadFile(restoredLockFile);

            return JsonUtility.Deserialize<DependencyLock>(content);
        }

        public static Task<DependencyLock> Load(string docset, CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (errors, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            return Load(docset, config.DependencyLock);
        }
    }
}
