// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        private readonly bool _stable;
        private readonly string _outputPath;

        /// <summary>
        /// Gets the logger to write logs, report diagnostics and progress.
        /// </summary>
        public ILog Log { get; }

        public Context(ILog log, string outputPath, bool stable)
        {
            Log = log;
            _stable = stable;
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

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            return File.OpenWrite(destinationPath);
        }

        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteJson(object graph, string destRelativePath)
        {
            using (var writer = new StreamWriter(WriteStream(destRelativePath)))
            {
                JsonUtililty.Serialize(writer, graph, _stable);
            }
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

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }
}
