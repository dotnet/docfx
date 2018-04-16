// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class FileSystem : IFileSystem
    {
        public bool Exists(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return File.Exists(Path.Combine(docsetPath, relativePath));
        }

        public Stream Read(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return File.OpenRead(Path.Combine(docsetPath, relativePath));
        }

        public Stream Write(string relativePath, string outputPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            var destinationPath = Path.Combine(outputPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            return File.OpenWrite(destinationPath);
        }

        public void Copy(string sourceRelativePath, string destRelativePath, string docsetPath, string outputPath)
        {
            Debug.Assert(!Path.IsPathRooted(sourceRelativePath));
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var sourcePath = Path.Combine(docsetPath, sourceRelativePath);
            var destinationPath = Path.Combine(outputPath, destRelativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }
}
