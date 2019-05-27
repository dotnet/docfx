// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class Output
    {
        private readonly string _outputPath;

        public Output(string outputPath)
        {
            _outputPath = Path.GetFullPath(outputPath);
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
            var sourcePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);

            File.Copy(sourcePath, GetDestinationPath(destRelativePath), overwrite: true);
        }

        public void Delete(string destRelativePath, bool legacy = false)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

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

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            return destinationPath;
        }
    }
}
