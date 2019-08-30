// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class DependencyLockProvider
    {
        public static string GetGitLock(Dictionary<string, string> dependencyLock, string href, string branch)
        {
            if (dependencyLock == null)
            {
                return null;
            }

            if (dependencyLock.TryGetValue($"{href}#{branch}", out var commit))
            {
                return commit;
            }

            if (branch == "master" && dependencyLock.TryGetValue($"{href}", out commit))
            {
                return commit;
            }

            return null;
        }

        public static Dictionary<string, string> Load(string docset, SourceInfo<string> dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            // dependency lock path can be a place holder for saving usage
            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                if (!File.Exists(Path.Combine(docset, dependencyLockPath)))
                {
                    return null;
                }
            }

            var content = RestoreFileMap.GetRestoredFileContent(docset, dependencyLockPath, fallbackDocset: null);

            return JsonUtility.Deserialize<Dictionary<string, string>>(content, new FilePath(dependencyLockPath));
        }

        public static void Save(string docset, string dependencyLockPath, Dictionary<string, string> dependencyLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(dependencyLockPath));

            var content = JsonUtility.Serialize(dependencyLock, indent: true);

            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                var path = Path.Combine(docset, dependencyLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ProcessUtility.WriteFile(path, content);
            }
        }
    }
}
