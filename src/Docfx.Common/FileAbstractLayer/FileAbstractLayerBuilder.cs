// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class FileAbstractLayerBuilder
{
    public static readonly FileAbstractLayerBuilder Default = new(EmptyFileReader.Instance, null);
    private readonly IFileReader _reader;
    private readonly IFileWriter _writer;

    private FileAbstractLayerBuilder(IFileReader reader, IFileWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public FileAbstractLayerBuilder ReadFromRealFileSystem(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return new FileAbstractLayerBuilder(new RealFileReader(folder), _writer);
    }

    public FileAbstractLayerBuilder ReadFromManifest(Manifest manifest, string manifestFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        return new FileAbstractLayerBuilder(new ManifestFileReader(manifest, manifestFolder), _writer);
    }

    public FileAbstractLayerBuilder WriteToManifest(Manifest manifest, string manifestFolder, string outputFolder = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        return new FileAbstractLayerBuilder(_reader, new ManifestFileWriter(manifest, manifestFolder, outputFolder));
    }

    public FileAbstractLayerBuilder ReadFromOutput(FileAbstractLayer fal)
    {
        ArgumentNullException.ThrowIfNull(fal);

        return new FileAbstractLayerBuilder(fal.Writer.CreateReader(), _writer);
    }

    public FileAbstractLayerBuilder WriteToRealFileSystem(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return new FileAbstractLayerBuilder(_reader, new RealFileWriter(folder));
    }

    public FileAbstractLayer Create()
    {
        return new FileAbstractLayer(_reader, _writer);
    }
}
