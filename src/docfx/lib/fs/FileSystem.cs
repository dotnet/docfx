// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class FileSystem
    {
        private readonly string _outputDirectory;

        public FileSystem(string outputDirectory) => _outputDirectory = Path.GetFullPath(outputDirectory);

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="relativePath">File path relative to docset</param>
        /// <param name="docsetPath">Docset path</param>
        public bool Exists(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return File.Exists(Path.Combine(docsetPath, relativePath));
        }

        /// <summary>
        /// Reads a file as stream, throws if it does not exists.
        /// </summary>
        /// <param name="relativePath">File path relative to docset</param>
        /// <param name="docsetPath">Docset path</param>
        public Stream Read(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return File.OpenRead(Path.Combine(docsetPath, relativePath));
        }

        /// <summary>
        /// Opens a write stream to write to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        /// <param name="relativePath">File path relative to <paramref name="outputPath"/></param>
        /// <param name="outputPath">Output path</param>
        public Stream Write(string relativePath, string outputPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            var destinationPath = Path.Combine(outputPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            return File.OpenWrite(destinationPath);
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        /// <param name="sourceRelativePath">Source file path relative to docset</param>
        /// <param name="destRelativePath">Destination file path relative to <paramref name="outputPath"/></param>
        /// <param name="docsetPath">Docset path</param>
        public void Copy(string sourceRelativePath, string destRelativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(sourceRelativePath));
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var sourcePath = Path.Combine(docsetPath, sourceRelativePath);
            var outputPath = Path.Combine(_outputDirectory, destRelativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            File.Copy(sourcePath, outputPath, overwrite: true);
        }
    }
}
