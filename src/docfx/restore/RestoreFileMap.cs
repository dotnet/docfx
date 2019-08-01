// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreFileMap
    {
        private readonly Docset _docset;

        public RestoreFileMap(Docset docset)
        {
            Debug.Assert(docset != null);
            _docset = docset;
        }

        public string GetRestoredFileContent(SourceInfo<string> url)
        {
            return GetRestoredFileContent(_docset.DocsetPath, url, _docset.FallbackDocset?.DocsetPath);
        }

        public string GetRestoredFilePath(SourceInfo<string> url)
        {
            var fromUrl = UrlUtility.IsHttp(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(_docset.DocsetPath, url);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                if (!string.IsNullOrEmpty(_docset.FallbackDocset?.DocsetPath))
                {
                    fullPath = Path.Combine(_docset.FallbackDocset?.DocsetPath, url);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }

                throw Errors.FileNotFound(url).ToException();
            }

            var filePath = RestoreFile.GetRestorePathFromUrl(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return filePath;
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

            var filePath = RestoreFile.GetRestorePathFromUrl(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            using (InterProcessMutex.Create(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }
    }
}
