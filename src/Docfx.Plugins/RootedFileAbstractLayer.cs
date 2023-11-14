// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public class RootedFileAbstractLayer : IFileAbstractLayer
{
    private readonly IFileAbstractLayer _impl;

    public RootedFileAbstractLayer(IFileAbstractLayer impl)
    {
        _impl = impl;
    }

    public IEnumerable<string> GetAllInputFiles() => _impl.GetAllInputFiles();

    public bool Exists(string file) =>
        Path.IsPathRooted(file) ? File.Exists(file) : _impl.Exists(file);

    public Stream OpenRead(string file) =>
        Path.IsPathRooted(file) ? File.OpenRead(file) : _impl.OpenRead(file);

    public Stream Create(string file)
    {
        if (Path.IsPathRooted(file))
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return File.Create(file);
        }
        return _impl.Create(file);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
        if (Path.IsPathRooted(sourceFileName) || Path.IsPathRooted(destFileName))
        {
            var dir = Path.GetDirectoryName(destFileName);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Copy(sourceFileName, destFileName, true);
            File.SetAttributes(destFileName, FileAttributes.Normal);
        }
        else
        {
            _impl.Copy(sourceFileName, destFileName);
        }
    }

    public string GetPhysicalPath(string file) =>
        Path.IsPathRooted(file) ? file : _impl.GetPhysicalPath(file);

    public string GetExpectedPhysicalPath(string file) =>
        Path.IsPathRooted(file) ? file : _impl.GetExpectedPhysicalPath(file);
}
