// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Common;

public class RealFileReader : IFileReader
{
    private readonly string _expandedInputFolder;

    public RealFileReader(string inputFolder, ImmutableDictionary<string, string> properties)
    {
        if (inputFolder == null)
        {
            throw new ArgumentNullException(nameof(inputFolder));
        }
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));

        _expandedInputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(inputFolder));
        if (!Directory.Exists(_expandedInputFolder))
        {
            throw new DirectoryNotFoundException($"Directory ({inputFolder}) not found.");
        }
        if (inputFolder.Length > 0 &&
            !inputFolder.EndsWith("\\", StringComparison.Ordinal) &&
            !inputFolder.EndsWith("/", StringComparison.Ordinal))
        {
            inputFolder += "/";
        }
        InputFolder = inputFolder;
    }

    public string InputFolder { get; }

    public ImmutableDictionary<string, string> Properties { get; }

    #region IFileReader Members

    public PathMapping? FindFile(RelativePath file)
    {
        var pp = Path.Combine(_expandedInputFolder, file.RemoveWorkingFolder());
        if (!File.Exists(pp))
        {
            return null;
        }
        return new PathMapping(file, Path.Combine(InputFolder, file.RemoveWorkingFolder())) { Properties = Properties };
    }

    public IEnumerable<RelativePath> EnumerateFiles()
    {
        var length = _expandedInputFolder.Length + 1;
        return from f in Directory.EnumerateFiles(_expandedInputFolder, "*.*", SearchOption.AllDirectories)
               select ((RelativePath)f.Substring(length)).GetPathFromWorkingFolder();
    }

    public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file) =>
        new[] { Path.Combine(InputFolder, file.RemoveWorkingFolder().ToString()) };

    #endregion
}
