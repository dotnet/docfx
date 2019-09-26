// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreFileMap
    {
        private readonly Input _input;

        public RestoreFileMap(Input input)
        {
            Debug.Assert(input != null);

            _input = input;
        }

        public FilePath GetRestoredFilePath(SourceInfo<string> url)
        {
            var fromUrl = UrlUtility.IsHttp(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var localFilePath = new FilePath(url, FileOrigin.Default);
                if (_input.Exists(localFilePath))
                {
                    return localFilePath;
                }

                localFilePath = new FilePath(url, FileOrigin.Fallback);
                if (_input.Exists(localFilePath))
                {
                    return localFilePath;
                }

                throw Errors.FileNotFound(url).ToException();
            }

            var filePath = RestoreFile.GetRestorePathFromUrl(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return new FilePath(filePath);
        }

        public string GetRestoredFileContent(SourceInfo<string> url)
        {
            return GetRestoredFileContent(_input, url);
        }

        public static string GetRestoredFileContent(Input input, SourceInfo<string> url)
        {
            var fromUrl = UrlUtility.IsHttp(url);
            if (!fromUrl)
            {
                var localFilePath = new FilePath(url, FileOrigin.Default);
                if (input.Exists(localFilePath))
                {
                    return input.ReadString(localFilePath);
                }

                localFilePath = new FilePath(url, FileOrigin.Fallback);
                if (input.Exists(localFilePath))
                {
                    return input.ReadString(localFilePath);
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
