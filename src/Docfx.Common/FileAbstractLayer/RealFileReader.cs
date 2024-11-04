// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class RealFileReader : IFileReader
{
    private readonly string _inputFolder;
    private readonly string _expandedInputFolder;

    public RealFileReader(string inputFolder)
    {
        ArgumentNullException.ThrowIfNull(inputFolder);

        _expandedInputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(inputFolder));
        if (!Directory.Exists(_expandedInputFolder))
        {
            throw new DirectoryNotFoundException($"Directory ({inputFolder}) not found.");
        }
        if (inputFolder.Length > 0 &&
            !inputFolder.EndsWith('\\') &&
            !inputFolder.EndsWith('/'))
        {
            inputFolder += "/";
        }
        _inputFolder = inputFolder;
    }

    public PathMapping? FindFile(RelativePath file)
    {
        var pp = Path.Combine(_expandedInputFolder, file.RemoveWorkingFolder());
        if (!File.Exists(pp))
        {
            return null;
        }
        return new PathMapping(file, Path.Combine(_inputFolder, file.RemoveWorkingFolder()));
    }

    public IEnumerable<RelativePath> EnumerateFiles()
    {
        var length = _expandedInputFolder.Length + 1;
        return from f in Directory.EnumerateFiles(_expandedInputFolder, "*.*", SearchOption.AllDirectories)
               select ((RelativePath)f.Substring(length)).GetPathFromWorkingFolder();
    }

    public string GetExpectedPhysicalPath(RelativePath file) =>
        Path.Combine(_inputFolder, file.RemoveWorkingFolder().ToString());
}
