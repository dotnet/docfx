// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

#nullable enable

/// <summary>
/// Clean command context class to execute <see cref="RunClean.Exec(RunCleanContext, CancellationToken)/>.
/// </summary>
internal class RunCleanContext
{
    private int _deletedFilesCount = 0;
    private int _skippedFilesCount = 0;

    /// <summary>
    /// config file directory.
    /// `docfx clean`cleanup files/directories under this
    /// </summary>
    public required string ConfigDirectory { get; init; }

    /// <summary>
    /// Output directory of `docfx build` command.
    /// </summary>
    public required string BuildOutputDirectory { get; init; }

    /// <summary>
    /// Output directories of `docfx metadata` command.
    /// </summary>
    public required string[] MetadataOutputDirectories { get; init; }

    /// <summary>
    /// If enabled. Skip files/directories delete operations.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Gets deleted files count.
    /// </summary>
    /// <remarks>
    public int DeletedFilesCount => _deletedFilesCount;

    /// <summary>
    /// Gets skipped files count.
    /// </summary>
    public int SkippedFilesCount => _skippedFilesCount;

    /// <summary>
    /// Increment <see cref="DeletedFilesCount"/>.
    /// </summary>
    public void IncrementDeletedFilesCount() => Interlocked.Increment(ref _deletedFilesCount);

    /// <summary>
    /// Increment <see cref="SkippedFilesCount"/>.
    /// </summary>
    public void IncrementSkippedFilesCount() => Interlocked.Increment(ref _skippedFilesCount);
}
