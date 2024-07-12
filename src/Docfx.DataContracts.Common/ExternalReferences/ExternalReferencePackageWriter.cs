// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

using Docfx.Common;

namespace Docfx.DataContracts.Common;

public class ExternalReferencePackageWriter : IDisposable
{
    private readonly ZipArchive _zip;

    private ExternalReferencePackageWriter(string packageFile, bool append)
    {
        if (append && File.Exists(packageFile))
        {
            _zip = new ZipArchive(new FileStream(packageFile, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update);
        }
        else
        {
            _zip = new ZipArchive(new FileStream(packageFile, FileMode.Create, FileAccess.ReadWrite), ZipArchiveMode.Create);
        }
    }

    public static ExternalReferencePackageWriter Create(string packageFile, Uri baseUri)
    {
        return new ExternalReferencePackageWriter(packageFile, false);
    }

    public static ExternalReferencePackageWriter Append(string packageFile, Uri baseUri)
    {
        return new ExternalReferencePackageWriter(packageFile, true);
    }

    public void AddOrUpdateEntry(string entryName, List<ReferenceViewModel> vm)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        ArgumentNullException.ThrowIfNull(vm);

        if (vm.Count == 0)
        {
            throw new ArgumentException("Empty collection is not allowed.", nameof(vm));
        }
        ZipArchiveEntry entry = null;
        if (_zip.Mode == ZipArchiveMode.Update)
        {
            entry = _zip.GetEntry(entryName);
        }
        entry ??= _zip.CreateEntry(entryName);
        using var stream = entry.Open();
        using var sw = new StreamWriter(stream);
        YamlUtility.Serialize(sw, vm);
    }

    public void Dispose()
    {
        _zip.Dispose();
    }
}
