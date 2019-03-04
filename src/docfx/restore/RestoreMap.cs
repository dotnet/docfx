// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreMap
    {
        public static Task<(string localPath, string content, string etag)> GetFileRestorePath(this Docset docset, string url)
        {
            return GetFileRestorePath(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static async Task<(string localPath, string content, string etag)> GetFileRestorePath(string docsetPath, string url, string fallbackDocset = null)
        {
            var fromUrl = HrefUtility.IsHttpHref(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return (fullPath, File.ReadAllText(fullPath), null);
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    return await GetFileRestorePath(fallbackDocset, url);
                }

                throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            var (content, etag) = await TryGetFileRestorePath(url);
            if (string.IsNullOrEmpty(content))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (null, content, etag);
        }

        public static async Task<(string content, string etag)> TryGetFileRestorePath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            var filePath = RestoreFile.GetRestorePath(url);
            string etag = null;
            string content = null;

            await ProcessUtility.RunInsideMutex(filePath, () =>
            {
                etag = GetEtag();
                content = File.Exists(filePath) ? File.ReadAllText(filePath) : null;

                return Task.CompletedTask;

                string GetEtag()
                {
                    var etagFile = $"{filePath}.etag";

                    if (File.Exists(etagFile))
                    {
                        return File.ReadAllText(etagFile);
                    }

                    return null;
                }
            });

            return (content, etag);
        }
    }
}
