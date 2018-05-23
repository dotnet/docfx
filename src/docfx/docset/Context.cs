// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public void ReportError(string code, string message, string file = null, int line = 0)
        {
            _reporter.Report(ReportLevel.Error, code, message, file, line);
        }

        public void ReportError(DocfxException exception)
        {
            _reporter.Report(ReportLevel.Error, exception.Code, exception.Message, exception.File, exception.Line, exception.Column);
        }

        public void ReportWarning(string code, string message, string file = null, int line = 0)
        {
            _reporter.Report(ReportLevel.Warning, code, message, file, line);
        }

        public void ReportWarning(DocfxException exception)
        {
            _reporter.Report(ReportLevel.Warning, exception.Code, exception.Message, exception.File, exception.Line, exception.Column);
        }

        public void ReportInfo(string code, string message, string file = null, int line = 0)
        {
            _reporter.Report(ReportLevel.Info, code, message, file, line);
        }

        public void ReportInfo(DocfxException exception)
        {
            _reporter.Report(ReportLevel.Info, exception.Code, exception.Message, exception.File, exception.Line, exception.Column);
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
    }
}
