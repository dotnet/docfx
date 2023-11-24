// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class RealFileWriter : FileWriterBase
{
    public RealFileWriter(string outputFolder)
        : base(outputFolder) { }

    public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
    {
        var dest = Path.Combine(ExpandedOutputFolder, destFileName.RemoveWorkingFolder());
        EnsureFolder(Path.GetDirectoryName(dest));
        var source = Environment.ExpandEnvironmentVariables(sourceFileName.PhysicalPath);
        if (!FilePathComparer.OSPlatformSensitiveStringComparer.Equals(source, dest))
        {
            File.Copy(source, dest, true);
        }
        File.SetAttributes(dest, FileAttributes.Normal);
    }

    public override Stream Create(RelativePath file)
    {
        var f = Path.Combine(ExpandedOutputFolder, file.RemoveWorkingFolder());
        EnsureFolder(Path.GetDirectoryName(f));
        return File.Create(f);
    }

    public override IFileReader CreateReader()
    {
        return new RealFileReader(OutputFolder);
    }
}
