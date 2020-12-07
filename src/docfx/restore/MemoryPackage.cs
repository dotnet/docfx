// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class MemoryPackage : Package
    {
        private readonly PathString _directory;

        private static AsyncLocal<ConcurrentDictionary<PathString, (DateTime lastWriteTime, string content)>> s_inMemoryFiles
            = new AsyncLocal<ConcurrentDictionary<PathString, (DateTime, string)>>();

        public override PathString BasePath => _directory;

        public MemoryPackage(string directory = ".")
        {
            _directory = new PathString(Path.GetFullPath(directory));
            if (s_inMemoryFiles.Value == null)
            {
                s_inMemoryFiles.Value = new ConcurrentDictionary<PathString, (DateTime, string)>();
            }
        }

        public void AddOrUpdate(PathString path, string content)
        {
            s_inMemoryFiles.Value!.AddOrUpdate(_directory.Concat(path), (key) => (DateTime.UtcNow, content), (key, oldValue) => (DateTime.UtcNow, content));
        }

        public override bool DirectoryExists(PathString directory = default)
        {
            var directoryFullPath = _directory.Concat(directory);
            return s_inMemoryFiles.Value!.Keys.Any(path => path.StartsWithPath(directoryFullPath, out _));
        }

        public override bool Exists(PathString path) => s_inMemoryFiles.Value!.ContainsKey(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        {
            var directoryPathString = _directory.Concat(new PathString(directory));
            var files = s_inMemoryFiles.Value!.Keys.Select((PathString file) =>
            {
                if (file.StartsWithPath(directoryPathString, out var relativePath))
                {
                    return relativePath;
                }
                else
                {
                    return default;
                }
            }).Where(file => file != default);

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
            if (s_inMemoryFiles.Value!.TryGetValue(_directory.Concat(path), out var value))
            {
                return value.lastWriteTime;
            }
            return null;
        }

        public override byte[] ReadBytes(PathString path)
        {
            var value = s_inMemoryFiles.Value!.GetValueOrDefault(_directory.Concat(path));
            return Encoding.UTF8.GetBytes(value.content ?? string.Empty);
        }

        public override Stream ReadStream(PathString path)
        {
            var value = s_inMemoryFiles.Value!.GetValueOrDefault(_directory.Concat(path));
            var byteArray = Encoding.UTF8.GetBytes(value.content ?? string.Empty);
            return new MemoryStream(byteArray);
        }

        public override PathString? TryGetPhysicalPath(PathString path) => null;
    }
}
