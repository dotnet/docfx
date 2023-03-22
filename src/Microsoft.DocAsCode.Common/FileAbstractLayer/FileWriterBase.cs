// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common;

public abstract class FileWriterBase : IFileWriter
{
    private const int MaxRetry = 3;

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
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
}
