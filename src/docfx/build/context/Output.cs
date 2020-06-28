// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Docs.Build
{
    internal class Output
    {
        private readonly Input _input;
        private readonly bool _dryRun;
        private readonly ActionBlock<Action> _queue = new ActionBlock<Action>(
            action => action(),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

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
        public void WriteJson(string destRelativePath, object graph)
        {
            EnsureNoDryRun();

            _queue.Post(() =>
            {
                using var writer = new StreamWriter(GetDestinationPath(destRelativePath));
                JsonUtility.Serialize(writer, graph);
            });
        }

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteText(string destRelativePath, string text)
        {
            EnsureNoDryRun();

            _queue.Post(() =>
            {
                File.WriteAllText(GetDestinationPath(destRelativePath), text);
            });
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void Copy(string destRelativePath, FilePath file)
        {
            EnsureNoDryRun();

            _queue.Post(() =>
            {
                var targetPhysicalPath = GetDestinationPath(destRelativePath);
                if (_input.TryGetPhysicalPath(file, out var sourcePhysicalPath))
                {
                    File.Copy(sourcePhysicalPath, targetPhysicalPath, overwrite: true);
                    return;
                }

                using var sourceStream = _input.ReadStream(file);
                using var targetStream = File.Create(targetPhysicalPath);
                sourceStream.CopyTo(targetStream);
                sourceStream.Flush();
            });
        }

        public void WaitForCompletion()
        {
            _queue.Complete();
            _queue.Completion.Wait();
        }

        public string GetDestinationPath(string destRelativePath)
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
