// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class RestoreFileMap
    {
        public static string GetRestoredFileContent(Docset docset, SourceInfo<string> url)
        {
            return GetRestoredFileContent(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static string GetRestoredFileContent(string docsetPath, SourceInfo<string> url, string fallbackDocset)
        {
            var fromUrl = UrlUtility.IsHttp(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    fullPath = Path.Combine(fallbackDocset, url);
                    if (File.Exists(fullPath))
                    {
                        return File.ReadAllText(fullPath);
                    }
                }

                throw Errors.FileNotFound(url).ToException();
            }

            var filePath = RestoreFile.GetRestoreContentPath(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            using (InterProcessMutex.Create(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }

        public static string GetRestoredFilePath(string docsetPath, SourceInfo<string> url, string fallbackDocset = null)
        {
            var fromUrl = UrlUtility.IsHttp(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    fullPath = Path.Combine(fallbackDocset, url);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }

                throw Errors.FileNotFound(url).ToException();
            }

            var filePath = RestoreFile.GetRestoreContentPath(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return filePath;
        }
    }
}
