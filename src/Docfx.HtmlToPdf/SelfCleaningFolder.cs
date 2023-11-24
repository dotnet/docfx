// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.HtmlToPdf;

public class SelfCleaningFolder : IDisposable
{
    public string FullPath { get; }

    public SelfCleaningFolder(string path)
    {
        Guard.ArgumentNotNullOrEmpty(path, nameof(path));

        path = Path.GetFullPath(path);
        Guard.Argument(() => !Directory.Exists(path), nameof(path), $"Directory already exists. Full path: {path}");

        Directory.CreateDirectory(path);
        FullPath = path;
    }

    public void Dispose()
    {
        if (Directory.Exists(FullPath))
        {
            FolderUtility.ForceDeleteDirectoryWithAllSubDirectories(FullPath);
        }
    }
}
