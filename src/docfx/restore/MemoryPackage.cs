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
    internal class MemoryPackage : DirectoryPackage
    {
        private static AsyncLocal<ConcurrentDictionary<PathString, (DateTime lastWriteTime, string content)>> s_inMemoryFiles
            = new AsyncLocal<ConcurrentDictionary<PathString, (DateTime, string)>>();

        public MemoryPackage(string directory = ".")
            : base(directory)
        {
            if (s_inMemoryFiles.Value == null)
            {
                s_inMemoryFiles.Value = new ConcurrentDictionary<PathString, (DateTime, string)>();
            }
        }

        public MemoryPackage(Dictionary<string, string> files, string directory = ".")
            : this(directory)
        {
            foreach (var (file, content) in files)
            {
                var fileFullPath = new PathString(Path.Combine(Directory, file));
                s_inMemoryFiles.Value!.AddOrUpdate(fileFullPath, (key) => (DateTime.UtcNow, content), (key, oldValue) => (DateTime.UtcNow, content));
            }
        }

        public void AddOrUpdate(PathString path, string content)
        {
            var fileFullPath = new PathString(Path.Combine(Directory, path));
            s_inMemoryFiles.Value!.AddOrUpdate(fileFullPath, (key) => (DateTime.UtcNow, content), (key, oldValue) => (DateTime.UtcNow, content));
        }

        public override DirectoryPackage CreateSubPackage(string relativePath) => new MemoryPackage(Path.Combine(Directory, relativePath));

        public override DateTime GetLastWriteTimeUtc(PathString path)
        {
            var fullPath = Directory.Concat(path);
            if (s_inMemoryFiles.Value!.TryGetValue(fullPath, out var value))
            {
                return value.lastWriteTime;
            }
            else
            {
                return base.GetLastWriteTimeUtc(path);
            }
        }

        public override bool Exists(PathString path)
        {
            var fullPath = Directory.Concat(path);
            if (s_inMemoryFiles.Value!.ContainsKey(fullPath))
            {
                return true;
            }
            else
            {
                return File.Exists(fullPath);
            }
        }

        public override bool DirectoryExists(PathString directory)
        {
            var fullPath = Directory.Concat(directory);
            if (s_inMemoryFiles.Value!.Keys.Any(path => path.StartsWithPath(fullPath, out _)))
            {
                return true;
            }
            return System.IO.Directory.Exists(fullPath);
        }

        public override Stream ReadStream(PathString path)
        {
            var fullPath = new PathString(Path.Combine(Directory, path));
            if (s_inMemoryFiles.Value!.TryGetValue(fullPath, out var value))
            {
                var byteArray = Encoding.UTF8.GetBytes(value.content ?? string.Empty);
                return new MemoryStream(byteArray);
            }
            else
            {
                return File.OpenRead(fullPath);
            }
        }

        public override IEnumerable<PathString> GetFiles(bool getFullPath = false, Func<string, bool>? fileNamePredicate = null)
        {
            var filesInMemory = s_inMemoryFiles.Value!.Keys.Select((PathString file) =>
            {
                if (file.StartsWithPath(Directory, out var relativePath))
                {
                    return relativePath;
                }
                else
                {
                    return default;
                }
            }).Where(file => file != default);
            if (fileNamePredicate != null)
            {
                filesInMemory = filesInMemory.Where((file) => fileNamePredicate.Invoke(Path.GetFileName(file)));
            }

            if (getFullPath)
            {
                filesInMemory = filesInMemory.Select(file => Directory.Concat(file));
            }

            if (System.IO.Directory.Exists(Directory))
            {
                return filesInMemory.Union(PathUtility.GetFilesInDirectory(Directory, getFullPath, fileNamePredicate));
            }
            return filesInMemory;
        }
    }
}
