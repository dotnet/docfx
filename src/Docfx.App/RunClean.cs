// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Docfx.Common;

#nullable enable

namespace Docfx;

/// <summary>
/// Helper class to cleanup docfx temporary files.
/// </summary>
internal static class RunClean
{
    private const string SearchPattern = "*";
    private static readonly EnumerationOptions DefaultEnumerationOptions = new()
    {
        MatchType = MatchType.Simple,
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
    };

    private static readonly StringComparison PathStringComparer = PathUtility.IsPathCaseInsensitive()
                                                                      ? StringComparison.OrdinalIgnoreCase
                                                                      : StringComparison.Ordinal;

    /// <summary>
    /// Cleanup docfx temporary files/directories.
    /// </summary>
    public static void Exec(RunCleanContext context, CancellationToken cancellationToken = default)
    {
        Logger.LogInfo("Clean operation started...");

        var startingTimestamp = Stopwatch.GetTimestamp();

        // Cleanup build output directory.
        var buildOutputDir = context.BuildOutputDirectory;
        if (!string.IsNullOrEmpty(buildOutputDir))
        {
            Logger.LogInfo($"Running clean operation on build output directory: {buildOutputDir}");
            CleanDirectoryContents(buildOutputDir, context, cancellationToken);
        }

        // Cleanup metadata output directories.
        foreach (var metadataOutputDir in context.MetadataOutputDirectories)
        {
            Logger.LogInfo($"Running clean operation on metadata output directory: {metadataOutputDir}");
            CleanDirectoryContents(metadataOutputDir, context, cancellationToken);
        }

        var elapsedSec = Stopwatch.GetElapsedTime(startingTimestamp).TotalSeconds;
        Logger.LogInfo($"Clean: {context.DeletedFilesCount} files are deleted, {context.SkippedFilesCount} files are skipped.");
    }

    /// <summary>
    /// Delete specified directory contents.
    /// </summary>
    private static void CleanDirectoryContents(string directoryPath, RunCleanContext context, CancellationToken cancellationToken = default)
    {
        Debug.Assert(Path.IsPathFullyQualified(directoryPath));

        if (!IsUnderConfigDirectoryPath(directoryPath, context.ConfigDirectory))
        {
            Logger.LogWarning($"Clean operation is not supported if the output directory is not located within a base directory that contains a `docfx.json`. path: {Path.GetFullPath(directoryPath)}");
            return;
        }

        var dirInfo = new DirectoryInfo(directoryPath);
        if (!dirInfo.Exists)
            return; // Skip if specified path is not exists.

        var configDirectory = context.ConfigDirectory;

        // Delete sub directories
        foreach (var subDirInfo in dirInfo.EnumerateDirectories(SearchPattern, DefaultEnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteDirectoryCore(subDirInfo, context, cancellationToken);
        }

        // Delete directory files
        foreach (var fileInfo in dirInfo.EnumerateFiles(SearchPattern, DefaultEnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteFileCore(fileInfo, context);
        }
    }

    /// <summary>
    /// Delete specified directory recursively.
    /// </summary>
    private static void DeleteDirectoryCore(DirectoryInfo dirInfo, RunCleanContext context, CancellationToken cancellationToken)
    {
        // Skip directory deletion, if specified directory have LinkTarget (SymbolicLink/DirectoryJunction).
        // Because it might cause unexpected deletion of file/directory or it might cause infinite loop.
        if (dirInfo.LinkTarget != null)
        {
            Logger.LogWarning("Enumeration of directory contents is skipped. Because it has LinkTarget. Path: " + dirInfo.FullName);
            return;
        }

        // Delete sub directories
        foreach (var subDirInfo in dirInfo.EnumerateDirectories(SearchPattern, DefaultEnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteDirectoryCore(subDirInfo, context, cancellationToken);
        }

        // Delete files
        foreach (var fileInfo in dirInfo.EnumerateFiles(SearchPattern, DefaultEnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteFileCore(fileInfo, context);
        }

        if (context.DryRun)
            return;

        // Try to delete root directory if there are no remaining files.
        if (dirInfo.GetFileSystemInfos("*", DefaultEnumerationOptions).Length == 0)
        {
            try
            {
                dirInfo.Delete(recursive: false);
            }
            catch
            {
                Logger.LogWarning("Skipped (Failed to delete): " + dirInfo.FullName);
                // Ignore exception. (File is being used by another process, has no permissions, or has readonly attribute)
            }
        }
    }

    private static void DeleteFileCore(FileInfo fileInfo, RunCleanContext context)
    {
        if (context.DryRun)
        {
            Logger.LogVerbose("Skipped: " + fileInfo.FullName);
            context.IncrementSkippedFilesCount();
            return;
        }

        if (fileInfo.LinkTarget != null)
        {
            Logger.LogWarning("File delete operation is skipped. Because it has LinkTarget. Path: " + fileInfo.FullName);
            context.IncrementSkippedFilesCount();
            return;
        }

        try
        {
            fileInfo.Delete();
            context.IncrementDeletedFilesCount();
        }
        catch
        {
            // File is being used by another process, has no permissions, or has readonly attribute.
            context.IncrementSkippedFilesCount();
            Logger.LogWarning("Skipped (Failed to delete): " + fileInfo.FullName);
        }
    }

    private static bool IsUnderConfigDirectoryPath(string targetPath, string basePath)
    {
        // Normalize paths
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
        targetPath = Path.GetFullPath(targetPath);

        // Try to append directory separator for string comparison.
        if (!targetPath.EndsWith(Path.DirectorySeparatorChar))
            targetPath += Path.DirectorySeparatorChar;

        return targetPath.StartsWith(basePath, PathStringComparer);
    }
}
