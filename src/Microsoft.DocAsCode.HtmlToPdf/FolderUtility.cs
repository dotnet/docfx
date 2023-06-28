// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.HtmlToPdf;

public static class FolderUtility
{
    public static void CopyDirectoryWithAllSubDirectories(string sourceDirectory, string targetDirectory, int maxDegreeOfParallelism = -1)
    {
        Guard.ArgumentNotNullOrEmpty(sourceDirectory, nameof(sourceDirectory));
        Guard.ArgumentNotNullOrEmpty(targetDirectory, nameof(targetDirectory));
        Guard.Argument(() => Directory.Exists(sourceDirectory), nameof(sourceDirectory), $"source directory '{sourceDirectory}' does not exist");

        Directory.CreateDirectory(targetDirectory);
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, targetDirectory));
        }

        Parallel.ForEach(
            Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories),
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            file => File.Copy(file, file.Replace(sourceDirectory, targetDirectory), true));
    }

    public static void ForceDeleteDirectoryWithAllSubDirectories(string directory, int maxDegreeOfParallelism = -1)
    {
        Guard.ArgumentNotNullOrEmpty(directory, nameof(directory));

        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                try
                {
                    ForceDeleteAllSubDirectories(directory, maxDegreeOfParallelism);
                    Directory.Delete(directory, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Delete directory {directory} failed: {ex.Message}");
                }
            }
        }
    }

    public static void ForceDeleteAllSubDirectories(string directory, int maxDegreeOfParallelism = -1)
    {
        Guard.ArgumentNotNullOrEmpty(directory, nameof(directory));

        if (Directory.Exists(directory))
        {
            Parallel.ForEach(
                Directory.GetFiles(directory, "*", SearchOption.AllDirectories),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                ForceDeleteFile);
        }
    }

    public static void ForceDeleteFile(string filePath)
    {
        Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

        try
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Delete File {filePath} failed: {ex.Message}");
        }
    }
}
