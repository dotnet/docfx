// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreFileMap
    {
        private readonly string _docsetPath;

        public RestoreFileMap(string docsetPath)
        {
            _docsetPath = docsetPath;
        }

        public string ReadString(SourceInfo<string> url)
        {
            using (var reader = new StreamReader(ReadStream(url)))
            {
                return reader.ReadToEnd();
            }
        }

        public Stream ReadStream(SourceInfo<string> url)
        {
            if (!UrlUtility.IsHttp(url))
            {
                var localFilePath = Path.Combine(_docsetPath, url);
                if (File.Exists(localFilePath))
                {
                    return File.OpenRead(localFilePath);
                }

                throw Errors.FileNotFound(url).ToException();
            }

            var filePath = RestoreFile.GetRestorePathFromUrl(url);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return File.OpenRead(filePath);
        }
    }
}
