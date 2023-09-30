// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class ManifestFileReader : IFileReader
{
    public Manifest Manifest { get; }

    public string ManifestFolder { get; }

    public ManifestFileReader(Manifest manifest, string manifestFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Manifest = manifest;
        ManifestFolder = manifestFolder;
    }

    public PathMapping? FindFile(RelativePath file)
    {
        OutputFileInfo entry;
        lock (Manifest)
        {
            entry = FindEntryInManifest(file.RemoveWorkingFolder());
        }
        if (entry == null)
        {
            return null;
        }
        return new PathMapping(file, entry.LinkToPath ?? Path.Combine(ManifestFolder, entry.RelativePath));
    }

    public IEnumerable<RelativePath> EnumerateFiles()
    {
        lock (Manifest)
        {
            return (from f in Manifest.Files
                    from ofi in f.Output.Values
                    select ((RelativePath)ofi.RelativePath).GetPathFromWorkingFolder()).Distinct().ToList();
        }
    }

    public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file)
    {
        OutputFileInfo entry;
        lock (Manifest)
        {
            entry = FindEntryInManifest(file.RemoveWorkingFolder());
        }
        if (entry == null)
        {
            return Enumerable.Empty<string>();
        }
        return new[] { entry.LinkToPath ?? Path.Combine(ManifestFolder, entry.RelativePath) };
    }

    private OutputFileInfo FindEntryInManifest(string file)
    {
        return Manifest.FindOutputFileInfo(file);
    }
}
