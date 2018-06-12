// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        private readonly string _outputPath;
        private readonly Reporter _reporter;

        public Context(Reporter reporter, string outputPath)
        {
            _reporter = reporter;
            _outputPath = Path.GetFullPath(outputPath);
        }

        /// <summary>
        /// Reports errors and warnings defined in <see cref="Errors"/>.
        /// </summary>
        public void Report(Document file, IEnumerable<DocfxException> exceptions)
        {
            var path = file.ToString();

            foreach (var error in exceptions)
            {
                Report(path == error.File || !string.IsNullOrEmpty(error.File)
                    ? error
                    : new DocfxException(error.Level, error.Code, error.Message, path, error.Line, error.Column, error.InnerException));
            }
        }

        /// <summary>
        /// Reports an error or warning defined in <see cref="Errors"/>.
        /// </summary>
        public void Report(DocfxException exception)
        {
            _reporter.Report(exception.Level, exception.Code, exception.Message, exception.File, exception.Line, exception.Column);
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

        public void Delete(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            File.Delete(destinationPath);
        }
    }
}
