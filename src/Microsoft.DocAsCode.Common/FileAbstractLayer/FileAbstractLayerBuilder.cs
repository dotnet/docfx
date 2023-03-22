// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Common;

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

    public FileAbstractLayerBuilder ReadFromRealFileSystem(string folder) =>
        ReadFromRealFileSystem(folder, ImmutableDictionary<string, string>.Empty);

    public FileAbstractLayerBuilder ReadFromRealFileSystem(string folder, ImmutableDictionary<string, string> properties)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder));
        }
        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }
        return new FileAbstractLayerBuilder(new RealFileReader(folder, properties), _writer);
    }

    public FileAbstractLayerBuilder ReadFromManifest(Manifest manifest, string manifestFolder)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }
        if (manifestFolder == null)
        {
            throw new ArgumentNullException(nameof(manifestFolder));
        }
        return new FileAbstractLayerBuilder(new ManifestFileReader(manifest, manifestFolder), _writer);
    }

    public FileAbstractLayerBuilder WriteToManifest(Manifest manifest, string manifestFolder, string outputFolder = null)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }
        if (manifestFolder == null)
        {
            throw new ArgumentNullException(nameof(manifestFolder));
        }
        return new FileAbstractLayerBuilder(_reader, new ManifestFileWriter(manifest, manifestFolder, outputFolder));
    }

    public FileAbstractLayerBuilder ReadFromOutput(FileAbstractLayer fal)
    {
        if (fal == null)
        {
            throw new ArgumentNullException(nameof(fal));
        }
        if (!fal.CanWrite)
        {
            throw new ArgumentException("FileAbstractLayer cannot write.", nameof(fal));
        }
        return new FileAbstractLayerBuilder(fal.Writer.CreateReader(), _writer);
    }

    public FileAbstractLayerBuilder WriteToRealFileSystem(string folder)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder));
        }
        return new FileAbstractLayerBuilder(_reader, new RealFileWriter(folder));
    }

    public FileAbstractLayer Create()
    {
        return new FileAbstractLayer(_reader, _writer);
    }
}
