// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Output
    {
        private readonly Input _input;

        public string OutputPath { get; }

        public Output(string outputPath, Input input)
        {
            OutputPath = Path.GetFullPath(outputPath);
            _input = input;
        }

        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteJson(object graph, string destRelativePath)
        {
            using (var writer = new StreamWriter(GetDestinationPath(destRelativePath)))
            {
                JsonUtility.Serialize(writer, graph);
            }
        }

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteText(string text, string destRelativePath)
        {
            File.WriteAllText(GetDestinationPath(destRelativePath), text);
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void Copy(Document file, string destRelativePath)
        {
            var targetPhysicalPath = GetDestinationPath(destRelativePath);
            if (_input.TryGetPhysicalPath(file.FilePath, out var sourcePhysicalPath))
            {
                File.Copy(sourcePhysicalPath, targetPhysicalPath, overwrite: true);
                return;
            }

            using (var sourceStream = _input.ReadStream(file.FilePath))
            using (var targetStream = File.Create(targetPhysicalPath))
            {
                sourceStream.CopyTo(targetStream);
            }
        }

        public void Delete(string destRelativePath, bool legacy = false)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(OutputPath, destRelativePath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            if (legacy)
            {
                var mtaJsonPath = LegacyUtility.ChangeExtension(destinationPath, "mta.json");
                if (File.Exists(mtaJsonPath))
                {
                    File.Delete(mtaJsonPath);
                }
            }
        }

        private string GetDestinationPath(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(OutputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            return destinationPath;
        }
    }
}
