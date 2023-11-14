// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class ManifestFileReader : IFileReader
{
    private readonly Manifest _manifest;
    private readonly string _manifestFolder;
    private readonly Dictionary<string, OutputFileInfo> _files;

    public ManifestFileReader(Manifest manifest, string manifestFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _manifest = manifest;
        _manifestFolder = manifestFolder;
        _files = ToLookup(manifest);
    }

    internal static Dictionary<string, OutputFileInfo> ToLookup(Manifest manifest)
    {
        return (
            from file in manifest.Files
            from output in file.Output
            group output.Value by output.Value.RelativePath)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public PathMapping? FindFile(RelativePath file)
    {
        lock (_manifest)
        {
            if (!_files.TryGetValue(file.RemoveWorkingFolder(), out var entry))
                return null;

            return new PathMapping(file, entry.LinkToPath ?? Path.Combine(_manifestFolder, entry.RelativePath));
        }
    }

    public IEnumerable<RelativePath> EnumerateFiles()
    {
        lock (_manifest)
        {
            return (from f in _manifest.Files
                    from ofi in f.Output.Values
                    select ((RelativePath)ofi.RelativePath).GetPathFromWorkingFolder()).Distinct().ToList();
        }
    }

    public string GetExpectedPhysicalPath(RelativePath file)
    {
        lock (_manifest)
        {
            if (!_files.TryGetValue(file.RemoveWorkingFolder(), out var entry))
                return null;

            return entry.LinkToPath ?? Path.Combine(_manifestFolder, entry.RelativePath);
        }
    }
}
