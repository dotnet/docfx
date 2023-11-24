// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class ManifestFileWriter : FileWriterBase
{
    private readonly bool _noRandomFile;
    private readonly Manifest _manifest;
    private readonly string _manifestFolder;
    private readonly Dictionary<string, OutputFileInfo> _files;

    public ManifestFileWriter(Manifest manifest, string manifestFolder, string outputFolder)
        : base(outputFolder ?? manifestFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        _manifest = manifest;
        _manifestFolder = manifestFolder;
        _noRandomFile = outputFolder == null;
        _files = ManifestFileReader.ToLookup(manifest);
    }

    public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
    {
        lock (_manifest)
        {
            _files[destFileName.RemoveWorkingFolder()].LinkToPath = sourceFileName.PhysicalPath;
        }
    }

    public override Stream Create(RelativePath file)
    {
        lock (_manifest)
        {
            var entry = _files[file.RemoveWorkingFolder()];
            if (entry == null)
            {
                throw new InvalidOperationException("File entry not found.");
            }
            if (_noRandomFile)
            {
                Directory.CreateDirectory(
                    Path.Combine(_manifestFolder, file.RemoveWorkingFolder().GetDirectoryPath()));
                var result = File.Create(Path.Combine(_manifestFolder, file.RemoveWorkingFolder()));
                entry.LinkToPath = null;
                return result;
            }
            else
            {
                var path = Path.Combine(OutputFolder, file.RemoveWorkingFolder());
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var result = File.Create(path);
                entry.LinkToPath = path;
                return result;
            }
        }
    }

    public override IFileReader CreateReader()
    {
        return new ManifestFileReader(_manifest, _manifestFolder);
    }
}
