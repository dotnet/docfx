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
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        public string ReadAllText(string path)
        {
            var sourcePath = Path.Combine(_outputPath, path);
            return File.ReadAllText(sourcePath);
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

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        public void Move(string sourceFileName, string destFileName)
        {
            Debug.Assert(!Path.IsPathRooted(destFileName));

            var sourcePath = Path.Combine(_outputPath, sourceFileName);
            var destinationPath = Path.Combine(_outputPath, destFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            File.Move(sourcePath, destinationPath);
        }
    }
}
