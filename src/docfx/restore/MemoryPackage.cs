// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class MemoryPackage : Package
    {
        private readonly PathString _directory;

        private ConcurrentDictionary<PathString, (DateTime lastWriteTime, string content)> _inMemoryFiles
            = new ConcurrentDictionary<PathString, (DateTime, string)>();

        public override PathString BasePath => _directory;

        public MemoryPackage(string directory = ".")
        {
            _directory = new PathString(Path.GetFullPath(directory));
        }

        public void AddOrUpdate(PathString path, string content)
        {
            _inMemoryFiles.AddOrUpdate(_directory.Concat(path), (key) => (DateTime.UtcNow, content), (key, oldValue) => (DateTime.UtcNow, content));
        }

        public void RemoveFile(PathString path) => _inMemoryFiles.TryRemove(_directory.Concat(path), out _);

        public override bool Exists(PathString path) => _inMemoryFiles.ContainsKey(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        {
            var directoryPathString = _directory.Concat(new PathString(directory));
            var files = _inMemoryFiles.Keys.Select((PathString file)
                => file.StartsWithPath(directoryPathString, out var relativePath) ? relativePath : default)
                .Where(file => file != default);

            if (allowedFileNames != null)
            {
                files = files.Where((file) =>
                {
                    var fileName = Path.GetFileName(file);
                    return allowedFileNames.Any((allowedFileName) => fileName.Equals(allowedFileName, StringComparison.OrdinalIgnoreCase));
                });
            }
            return files;
        }

        public override PathString GetFullFilePath(PathString path) => new PathString(_directory.Concat(path));

        public override DateTime? TryGetLastWriteTimeUtc(PathString path)
        {
            if (_inMemoryFiles.TryGetValue(_directory.Concat(path), out var value))
            {
                return value.lastWriteTime;
            }
            return null;
        }

        public override byte[] ReadBytes(PathString path)
        {
            var value = _inMemoryFiles.GetValueOrDefault(_directory.Concat(path));
            return Encoding.UTF8.GetBytes(value.content ?? string.Empty);
        }

        public override Stream ReadStream(PathString path)
        {
            var value = _inMemoryFiles.GetValueOrDefault(_directory.Concat(path));
            var byteArray = Encoding.UTF8.GetBytes(value.content);
            return new MemoryStream(byteArray);
        }

        public override PathString? TryGetPhysicalPath(PathString path) => null;
    }
}
