// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public abstract class FileWriterBase : IFileWriter
{
    public FileWriterBase(string outputFolder)
    {
        ExpandedOutputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder));
        EnsureFolder(ExpandedOutputFolder);
        OutputFolder = outputFolder;
    }

    public string OutputFolder { get; }

    public string ExpandedOutputFolder { get; }

    public abstract void Copy(PathMapping sourceFileName, RelativePath destFileName);

    public abstract Stream Create(RelativePath filePath);

    public abstract IFileReader CreateReader();

    protected internal static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
    }
}
