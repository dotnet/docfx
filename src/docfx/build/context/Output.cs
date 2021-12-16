// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Docs.Build;

internal class Output
{
    private readonly Input _input;
    private readonly bool _dryRun;
    private readonly ActionBlock<Action> _queue = new(
        action => action(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

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
            using var stream = new FileStream(EnsureDestinationPath(destRelativePath), FileMode.Create);
            JsonUtility.SerializeStable(stream, graph);
        });
    }

    /// <summary>
    /// Writes the input text to an output file.
    /// Throws if multiple threads trying to write to the same destination concurrently.
    /// </summary>
    public void WriteText(string destRelativePath, string? text)
    {
        EnsureNoDryRun();

        if (text != null)
        {
            _queue.Post(() => File.WriteAllText(EnsureDestinationPath(destRelativePath), text));
        }
    }

    /// <summary>
    /// Writes the input lines to an output file.
    /// Throws if multiple threads trying to write to the same destination concurrently.
    /// </summary>
    public void WriteLines(string[] destRelativePaths, IEnumerable<string> lines)
    {
        EnsureNoDryRun();

        if (destRelativePaths.Length > 0)
        {
            _queue.Post(() =>
            {
                File.WriteAllLines(EnsureDestinationPath(destRelativePaths[0]), lines);
                for (var i = 1; i < destRelativePaths.Length; i++)
                {
                    File.Copy(EnsureDestinationPath(destRelativePaths[0]), EnsureDestinationPath(destRelativePaths[i]), overwrite: true);
                }
            });
        }
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
            var targetPhysicalPath = EnsureDestinationPath(destRelativePath);
            if (_input.TryGetPhysicalPath(file) is PathString sourcePhysicalPath)
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

    /// <summary>
    /// Copies a file from source to destination, throws if source does not exists.
    /// Throws if multiple threads trying to write to the same destination concurrently.
    /// </summary>
    public void Copy(string destRelativePath, Package package, PathString sourcePath)
    {
        EnsureNoDryRun();

        _queue.Post(() =>
        {
            var targetPhysicalPath = EnsureDestinationPath(destRelativePath);
            var sourcePhysicalPath = package.TryGetPhysicalPath(sourcePath);
            if (sourcePhysicalPath != null)
            {
                File.Copy(sourcePhysicalPath, targetPhysicalPath, overwrite: true);
                return;
            }

            using var sourceStream = package.ReadStream(sourcePath);
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

    private string EnsureDestinationPath(string destRelativePath)
    {
        Debug.Assert(!Path.IsPathRooted(destRelativePath));

        var destinationPath = Path.Combine(OutputPath, destRelativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath)) ?? ".");

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
