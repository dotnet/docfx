// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal interface IFileSystem
    {
        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="relativePath">File path relative to docset</param>
        /// <param name="docsetPath">Docset path</param>
        bool Exists(string relativePath, string docsetPath);

        /// <summary>
        /// Reads a file as stream, throws if it does not exists.
        /// </summary>
        /// <param name="relativePath">File path relative to docset</param>
        /// <param name="docsetPath">Docset path</param>
        Stream Read(string relativePath, string docsetPath);

        /// <summary>
        /// Opens a write stream to write to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        /// <param name="relativePath">File path relative to <paramref name="outputPath"/></param>
        /// <param name="outputPath">Output path</param>
        Stream Write(string relativePath, string outputPath);

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        /// <param name="sourceRelativePath">Source file path relative to docset</param>
        /// <param name="destRelativePath">Destination file path relative to <paramref name="outputPath"/></param>
        /// <param name="docsetPath">Docset path</param>
        /// <param name="outputPath">Output path</param>
        void Copy(string sourceRelativePath, string destRelativePath, string docsetPath, string outputPath);
    }
}
