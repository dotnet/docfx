// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class FileAbstractLayer : IFileAbstractLayer
{
    public FileAbstractLayer(IFileReader reader, IFileWriter writer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Reader = reader;
        Writer = writer;
    }

    public IFileReader Reader { get; }

    public IFileWriter Writer { get; }

    public IEnumerable<RelativePath> GetAllInputFiles()
    {
        return Reader.EnumerateFiles();
    }

    public bool Exists(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Reader.FindFile(file) != null;
    }

    public Stream OpenRead(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var pp = FindPhysicalPath(file);
        return File.OpenRead(Environment.ExpandEnvironmentVariables(pp.PhysicalPath));
    }

    public Stream Create(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Writer.Create(file);
    }

    public void Copy(RelativePath sourceFileName, RelativePath destFileName)
    {
        ArgumentNullException.ThrowIfNull(sourceFileName);
        ArgumentNullException.ThrowIfNull(destFileName);

        Writer.Copy(FindPhysicalPath(sourceFileName), destFileName);
    }

    public string GetPhysicalPath(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return FindPhysicalPath(file).PhysicalPath;
    }

    public string GetExpectedPhysicalPath(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Reader.GetExpectedPhysicalPath(file);
    }

    IEnumerable<string> IFileAbstractLayer.GetAllInputFiles() =>
        from r in GetAllInputFiles()
        select (string)r.RemoveWorkingFolder();

    public bool Exists(string file) =>
        Exists((RelativePath)file);

    public Stream OpenRead(string file) =>
        OpenRead((RelativePath)file);

    public Stream Create(string file) =>
        Create((RelativePath)file);

    public void Copy(string sourceFileName, string destFileName) =>
        Copy((RelativePath)sourceFileName, (RelativePath)destFileName);

    public string GetPhysicalPath(string file) =>
        GetPhysicalPath((RelativePath)file);

    public string GetExpectedPhysicalPath(string file) =>
        GetExpectedPhysicalPath((RelativePath)file);

    private PathMapping FindPhysicalPath(RelativePath file)
    {
        var mapping = Reader.FindFile(file);
        if (mapping == null)
        {
            string fn = file;
            throw new FileNotFoundException($"File ({fn}) not found.", fn);
        }
        return mapping.Value;
    }
}
