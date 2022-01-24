// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text;

namespace Microsoft.Docs.Build;

internal class MemoryPackage : Package
{
    private readonly PathString _directory;
    private readonly ConcurrentDictionary<PathString, (DateTime lastWriteTime, string content)> _inMemoryFiles = new();

    public override PathString BasePath => _directory;

    public MemoryPackage(string directory = ".") => _directory = new(Path.GetFullPath(directory));

    public void AddOrUpdate(PathString path, string content) => _inMemoryFiles[_directory.Concat(path)] = (DateTime.UtcNow, content);

    public override bool Exists(PathString path) => _inMemoryFiles.ContainsKey(_directory.Concat(path));

    public IEnumerable<PathString> GetAllFilesInMemory() => _inMemoryFiles.Keys;

    public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
    {
        var directoryPathString = _directory.Concat(new(directory));
        var files = _inMemoryFiles.Keys
            .Select(file => file.StartsWithPath(directoryPathString, out var relativePath) ? relativePath : default)
            .Where(file => file != default);

        if (allowedFileNames != null)
        {
            files = files.Where(file =>
            {
                var fileName = Path.GetFileName(file);
                return allowedFileNames.Any(allowedFileName => fileName.Equals(allowedFileName, StringComparison.OrdinalIgnoreCase));
            });
        }
        return files;
    }

    public override PathString GetFullFilePath(PathString path) => new(_directory.Concat(path));

    public override DateTime? TryGetLastWriteTimeUtc(PathString path) => _inMemoryFiles.GetValueOrDefault(_directory.Concat(path)).lastWriteTime;

    public override string ReadString(PathString path) => _inMemoryFiles[_directory.Concat(path)].content;

    public override byte[] ReadBytes(PathString path) => Encoding.UTF8.GetBytes(ReadString(path));

    public override Stream ReadStream(PathString path) => new MemoryStream(ReadBytes(path), writable: false);

    public void RemoveFile(PathString path) => _inMemoryFiles.TryRemove(_directory.Concat(path), out _);

    public override PathString? TryGetPhysicalPath(PathString path) => null;
}
