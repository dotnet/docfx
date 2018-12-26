// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        public readonly Cache Cache;
        public readonly Report Report;

        private readonly string _outputPath;

        public Context(Report report, Cache cache, string outputPath)
        {
            Report = report;
            Cache = cache;
            _outputPath = Path.GetFullPath(outputPath);
        }

        /// <summary>
        /// Opens a write stream to write to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public Stream WriteStream(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            return File.Create(destinationPath);
        }

        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteJson(object graph, string destRelativePath)
        {
            using (var writer = new StreamWriter(WriteStream(destRelativePath)))
            {
                JsonUtility.Serialize(writer, graph);
            }
        }

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteText(string contents, string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            File.WriteAllText(destinationPath, contents);
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void Copy(Document file, string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var sourcePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);
            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        public void Delete(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
    }
}
