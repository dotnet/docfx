// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class Output
    {
        private readonly Input _input;
        private readonly bool _dryRun;

        public string OutputPath { get; }

        public Output(string outputPath, Input input, bool dryRun)
        {
            OutputPath = Path.GetFullPath(outputPath);
            _input = input;
            _dryRun = dryRun;
        }

        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteJson(object graph, string destRelativePath)
        {
            EnsureNoDryRun();

            using var writer = new StreamWriter(GetDestinationPath(destRelativePath));
            JsonUtility.Serialize(writer, graph);
        }

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteText(string text, string destRelativePath)
        {
            EnsureNoDryRun();

            File.WriteAllText(GetDestinationPath(destRelativePath), text);
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void Copy(Document file, string destRelativePath)
        {
            EnsureNoDryRun();

            var targetPhysicalPath = GetDestinationPath(destRelativePath);
            if (_input.TryGetPhysicalPath(file.FilePath, out var sourcePhysicalPath))
            {
                File.Copy(sourcePhysicalPath, targetPhysicalPath, overwrite: true);
                return;
            }

            using var sourceStream = _input.ReadStream(file.FilePath);
            using var targetStream = File.Create(targetPhysicalPath);
            sourceStream.CopyTo(targetStream);
        }

        public void Delete(string destRelativePath, bool legacy = false)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            EnsureNoDryRun();

            var destinationPath = Path.Combine(OutputPath, destRelativePath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            if (legacy)
            {
                var mtaJsonPath = LegacyUtility.ChangeExtension(destinationPath, ".mta.json");
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

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath)));

            return destinationPath;
        }

        private void EnsureNoDryRun()
        {
            if (_dryRun)
            {
                throw new InvalidOperationException("Don't write output in --dry-run mode");
            }
        }
    }
}
