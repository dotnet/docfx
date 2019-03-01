// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class RestoreMap
    {
        private static readonly ConcurrentDictionary<(string url, string version), Lazy<string>> s_downloadPath = new ConcurrentDictionary<(string url, string version), Lazy<string>>();

        public static (bool fromUrl, string path) GetFileRestorePath(this Docset docset, string url)
        {
            return GetFileRestorePath(docset.DocsetPath, url, docset.DependencyLock?.Downloads.GetValueOrDefault(url), docset.FallbackDocset?.DocsetPath);
        }

        public static (bool fromUrl, string path) GetFileRestorePath(string docsetPath, string url, DependencyVersion dependencyVersion, string fallbackDocset = null)
        {
            var fromUrl = HrefUtility.IsHttpHref(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return (fromUrl, fullPath);
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    return GetFileRestorePath(fallbackDocset, url, dependencyVersion);
                }

                throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            if (!TryGetFileRestorePath(url, dependencyVersion, out var result))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (fromUrl, result);
        }

        public static bool TryGetFileRestorePath(string url, DependencyVersion dependencyVersion, out string result)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            var fileName = dependencyVersion?.Hash;
            var locked = !string.IsNullOrEmpty(fileName);
            result = s_downloadPath.AddOrUpdate(
                (url, fileName),
                new Lazy<string>(FindFile),
                (_, existing) => existing.Value != null ? existing : new Lazy<string>(FindFile)).Value;

            return File.Exists(result);

            string FindFile()
            {
                // get the file path from restore map
                var restoreDir = AppData.GetFileDownloadDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    return null;
                }

                // return specified version
                if (locked)
                {
                    return Path.Combine(restoreDir, fileName);
                }

                // return the latest version
                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                       .FirstOrDefault();
            }
        }
    }
}
