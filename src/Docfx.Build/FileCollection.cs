// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class FileCollection
{
    private readonly List<FileAndType> _files = [];

    public int Count => _files.Count;

    public FileCollection(string defaultBaseDir)
    {
        if (string.IsNullOrEmpty(defaultBaseDir))
        {
            DefaultBaseDir = Directory.GetCurrentDirectory();
        }
        else
        {
            DefaultBaseDir = Path.Combine(Directory.GetCurrentDirectory(), defaultBaseDir);
        }
    }

    public FileCollection(FileCollection collection) : this(collection.DefaultBaseDir)
    {
        _files = new List<FileAndType>(collection._files);
    }

    public string DefaultBaseDir { get; set; }

    public void Add(DocumentType type, IEnumerable<string> files, string sourceDir = null, string destinationDir = null)
    {
        Add(type, null, files, sourceDir, destinationDir);
    }

    public void Add(DocumentType type, string baseDir, IEnumerable<string> files, string sourceDir = null, string destinationDir = null)
    {
        var rootedBaseDir = Path.Combine(DefaultBaseDir, baseDir ?? string.Empty);
        if (sourceDir != null && Path.IsPathRooted(sourceDir))
        {
            if (sourceDir.StartsWith(rootedBaseDir, StringComparison.Ordinal))
            {
                sourceDir = sourceDir.Substring(rootedBaseDir.Length).TrimStart('/', '\\');
            }
            else
            {
                throw new ArgumentException("SourceDir must start with BaseDir, or relative path.", nameof(sourceDir));
            }
        }
        if (destinationDir != null && Path.IsPathRooted(destinationDir))
        {
            if (destinationDir.StartsWith(rootedBaseDir, StringComparison.Ordinal))
            {
                destinationDir = sourceDir.Substring(rootedBaseDir.Length).TrimStart('/', '\\');
            }
            else
            {
                throw new ArgumentException("DestinationDir must start with BaseDir, or relative path.", nameof(destinationDir));
            }
        }
        if (!string.IsNullOrEmpty(sourceDir) && !sourceDir.EndsWith('/'))
        {
            sourceDir += "/";
        }
        if (!string.IsNullOrEmpty(destinationDir) && !destinationDir.EndsWith('/'))
        {
            destinationDir += "/";
        }
        _files.AddRange(from f in files
                        select new FileAndType(rootedBaseDir, ToRelative(f, rootedBaseDir), type, sourceDir, destinationDir));
    }

    public void RemoveAll(Predicate<FileAndType> match)
    {
        _files.RemoveAll(match);
    }

    private static string ToRelative(string file, string rootedBaseDir)
    {
        if (!Path.IsPathRooted(file))
        {
            return file;
        }
        var result = PathUtility.MakeRelativePath(rootedBaseDir, file);
        if (Path.IsPathRooted(result))
        {
            throw new ArgumentException($"Cannot get relative path for {file} from {rootedBaseDir}.", nameof(file));
        }
        return result;
    }

    public IEnumerable<FileAndType> EnumerateFiles()
    {
        return _files.Distinct();
    }
}
