// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreMap
    {
        public static Task<(string localPath, string content, string etag)> GetRestoredFileContent(this Docset docset, string url)
        {
            return GetRestoredFileContent(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static async Task<(string localPath, string content, string etag)> GetRestoredFileContent(string docsetPath, string url, string fallbackDocset = null)
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
                    return await GetRestoredFileContent(fallbackDocset, url);
                }

                throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            var (content, etag) = await TryGetRestoredFileContent(url);
            if (string.IsNullOrEmpty(content))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (null, content, etag);
        }

        public static async Task<(string content, string etag)> TryGetRestoredFileContent(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            var filePath = RestoreFile.GetRestoreContentPath(url);
            var etagPath = RestoreFile.GetRestoreEtagPath(url);
            string etag = null;
            string content = null;

            await ProcessUtility.RunInsideMutex(filePath, () =>
            {
                content = GetFileContentIfExists(filePath);
                etag = GetFileContentIfExists(etagPath);

                return Task.CompletedTask;

                string GetFileContentIfExists(string file)
                {
                    if (File.Exists(file))
                    {
                        return File.ReadAllText(file);
                    }

                    return null;
                }
            });

            return (content, etag);
        }
    }
}
