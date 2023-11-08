// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Plugins;

namespace Docfx.Common;

public class FileAbstractLayer : IFileAbstractLayer, IDisposable
{
    #region Constructors

    public FileAbstractLayer(IFileReader reader, IFileWriter writer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Reader = reader;
        Writer = writer;
    }

    #endregion

    #region Public Members

    public IFileReader Reader { get; }

    public IFileWriter Writer { get; }

    public bool CanRead => !_disposed;

    public bool CanWrite => !_disposed && Writer != null;

    public IEnumerable<RelativePath> GetAllInputFiles()
    {
        EnsureNotDisposed();
        return Reader.EnumerateFiles();
    }

    public bool Exists(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureNotDisposed();
        return Reader.FindFile(file) != null;
    }

    public Stream OpenRead(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureNotDisposed();
        var pp = FindPhysicalPath(file);
        return File.OpenRead(Environment.ExpandEnvironmentVariables(pp.PhysicalPath));
    }

    public Stream Create(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureNotDisposed();
        if (!CanWrite)
        {
            throw new InvalidOperationException();
        }
        return Writer.Create(file);
    }

    public void Copy(RelativePath sourceFileName, RelativePath destFileName)
    {
        ArgumentNullException.ThrowIfNull(sourceFileName);
        ArgumentNullException.ThrowIfNull(destFileName);

        EnsureNotDisposed();
        if (!CanWrite)
        {
            throw new InvalidOperationException();
        }
        Writer.Copy(FindPhysicalPath(sourceFileName), destFileName);
    }

    public string GetPhysicalPath(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureNotDisposed();
        return FindPhysicalPath(file).PhysicalPath;
    }

    public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureNotDisposed();
        return Reader.GetExpectedPhysicalPath(file);
    }

    #endregion

    #region IFileAbstractLayer Members

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

    public ImmutableDictionary<string, string> GetProperties(string file) =>
        GetProperties((RelativePath)file);

    public string GetPhysicalPath(string file) =>
        GetPhysicalPath((RelativePath)file);

    public IEnumerable<string> GetExpectedPhysicalPath(string file) =>
        GetExpectedPhysicalPath((RelativePath)file);

    #endregion

    #region IDisposable Support

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion

    #region Private Methods

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

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("FileAbstractLayer");
        }
    }

    #endregion
}
