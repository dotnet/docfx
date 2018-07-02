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
        private readonly Report _report;

        public Context(Report report, string outputPath)
        {
            _report = report;
            _outputPath = Path.GetFullPath(outputPath);
        }

        /// <summary>
        /// Reports errors and warnings defined in <see cref="Errors"/>.
        /// </summary>
        public void Report(Document file, IEnumerable<Error> errors)
        {
            Report(file.ToString(), errors);
        }

        /// <summary>
        /// Reports errors and warnings defined in <see cref="Errors"/>.
        /// </summary>
        public void Report(string file, IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Report(file == error.File || !string.IsNullOrEmpty(error.File)
                    ? error
                    : new Error(error.Level, error.Code, error.Message, file, error.Line, error.Column));
            }
        }

        /// <summary>
        /// Reports an error or warning defined in <see cref="Errors"/>.
        /// </summary>
        public void Report(Error error)
        {
            _report.Write(error);
        }

        /// <summary>
        /// Convert relative path to absolute path
        /// </summary>
        /// <param name="relativePath">Relatived to _outputPath</param>
        public string ConvertToAbsolutePath(string relativePath)
        {
            return Path.Combine(_outputPath, relativePath);
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
