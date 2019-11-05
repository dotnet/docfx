// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreFileMap
    {
        private readonly Input _input;

        public RestoreFileMap(Input input)
        {
            _input = input;
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
            return ReadStream(_input, url);
        }

        public static string ReadString(Input input, SourceInfo<string> url)
        {
            using (var reader = new StreamReader(ReadStream(input, url)))
            {
                return reader.ReadToEnd();
            }
        }

        public static Stream ReadStream(Input input, SourceInfo<string> url)
        {
            if (!UrlUtility.IsHttp(url))
            {
                var localFilePath = new FilePath(url, FileOrigin.Default);
                if (input.Exists(localFilePath))
                {
                    return input.ReadStream(localFilePath);
                }

                localFilePath = new FilePath(url, FileOrigin.Fallback);
                if (input.Exists(localFilePath))
                {
                    return input.ReadStream(localFilePath);
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
