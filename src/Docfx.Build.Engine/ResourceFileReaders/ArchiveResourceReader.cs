// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

using Docfx.Common;

namespace Docfx.Build.Engine;

public sealed class ArchiveResourceReader : ResourceFileReader
{
    private readonly object _locker = new();
    private ZipArchive _zipped;
    private bool disposed = false;

    public override string Name { get; }
    public override IEnumerable<string> Names { get; }
    public override bool IsEmpty { get; }

    public ArchiveResourceReader(Stream stream, string name)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _zipped = new ZipArchive(stream);
        Name = name;
        // When Name is empty, entry is folder, ignore
        Names = _zipped.Entries.Where(s => !string.IsNullOrEmpty(s.Name)).Select(s => s.FullName);
        IsEmpty = !Names.Any();
    }

    /// <summary>
    /// TODO: This is not thread safe, only expose GetResource in interface
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public override Stream GetResourceStream(string name)
    {
        if (IsEmpty) return null;

        lock (_locker)
        {
            var memoryStream = new MemoryStream();
            using (var stream = GetResourceStreamCore(name))
            {
                if (stream == null)
                {
                    return null;
                }

                stream.CopyTo(memoryStream);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }

    public override string GetResource(string name)
    {
        lock (_locker)
        {
            using var stream = GetResourceStreamCore(name);
            return GetString(stream);
        }
    }

    private Stream GetResourceStreamCore(string name)
    {
        // zip entry is case sensitive
        // in case relative path is combined by backslash \
        return _zipped.GetEntry(StringExtension.ToNormalizedPath(name.Trim()))?.Open();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        _zipped?.Dispose();
        _zipped = null;
        disposed = true;

        base.Dispose(disposing);
    }
}
