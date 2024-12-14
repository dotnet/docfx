// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.Engine;

public sealed class LocalFileResourceReader : ResourceFileReader
{
    private const int MaxSearchLevel = 5;
    // keep comparer to be case sensitive as to be consistent with zip entries
    private static readonly StringComparer ResourceComparer = StringComparer.Ordinal;
    private readonly string _directory = null;
    private readonly int _maxDepth;
    private readonly HashSet<string> NamesHashSet;

    public override string Name { get; }
    public override IEnumerable<string> Names { get; }
    public override bool IsEmpty { get; }

    public LocalFileResourceReader(string directory, int maxSearchLevel = MaxSearchLevel)
    {
        if (string.IsNullOrEmpty(directory)) _directory = Directory.GetCurrentDirectory();
        else _directory = directory;
        Name = _directory;
        _maxDepth = maxSearchLevel;
        var includedFiles = GetFiles(_directory, "*", maxSearchLevel);
        Names = includedFiles.Select(s => PathUtility.MakeRelativePath(_directory, s)).Where(s => s != null).ToArray();
        NamesHashSet = Names.ToHashSet(ResourceComparer);

        IsEmpty = !Names.Any();
    }

    public override Stream GetResourceStream(string name)
    {
        if (IsEmpty) return null;

        // in case relative path is combined by backslash \
        var normalizedName = StringExtension.ToNormalizedPath(name.Trim());
        if (!NamesHashSet.Contains(normalizedName))
            return null;

        var filePath = Path.Combine(_directory, name);
        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    private IEnumerable<string> GetFiles(string directory, string searchPattern, int searchLevel)
    {
        if (searchLevel < 1)
        {
            return [];
        }
        var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
        var dirs = Directory.GetDirectories(directory);
        if (searchLevel == 1)
        {
            if (dirs.Length > 0)
            {
                var dirPaths = (from dir in dirs select PathUtility.MakeRelativePath(_directory, dir)).ToDelimitedString();
                Logger.LogInfo($"The following directories exceed max allowed depth {_maxDepth}, ignored: {dirPaths}.");
            }

            return files;
        }
        List<string> allFiles = new(files);
        foreach (var dir in dirs)
        {
            allFiles.AddRange(GetFiles(dir, searchPattern, searchLevel - 1));
        }
        return allFiles;
    }
}
