// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Utility;
    
    public sealed class ArchiveResourceCollection : ResourceCollection
    {
        private ZipArchive _zipped = null;

        public ArchiveResourceCollection(Stream stream)
        {
            _zipped = new ZipArchive(stream);

            // When Name is empty, entry is folder, ignore
            Names = _zipped.Entries.Where(s => !string.IsNullOrEmpty(s.Name)).Select(s => s.FullName);
        }

        public override Stream GetResourceStream(string name)
        {
            // Zip mode
            if (_zipped == null) return null;
            // zip entry is case sensitive
            // incase relative path is combined by backslash \
            return _zipped.GetEntry(name.ToNormalizedPath())?.Open();
        }

        public override void Dispose()
        {
            _zipped?.Dispose();
            base.Dispose();
        }

    }

    public sealed class FileResourceCollection : ResourceCollection
    {
        // keep comparer to be case sensitive as to be consistent with zip entries
        private static StringComparer ResourceComparer = StringComparer.Ordinal;
        private string _directory = null;

        public FileResourceCollection(string directory)
        {
            if (string.IsNullOrEmpty(directory)) _directory = Environment.CurrentDirectory;
            else _directory = directory;
            Names = Directory.GetFiles(_directory, "*", SearchOption.AllDirectories).Select(s => FileExtensions.MakeRelativePath(_directory, s));
        }

        public override Stream GetResourceStream(string name)
        {
            // Directory mode
            if (_directory == null) return null;

            // incase relative path is combined by backslash \
            if (!Names.Contains(name.ToNormalizedPath(), ResourceComparer)) return null;
            var filePath = Path.Combine(_directory, name);
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
    }

    public abstract class ResourceCollection : IDisposable
    {
        public IEnumerable<string> Names { get; set; }

        public string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
                return GetString(stream);
        }
        public abstract Stream GetResourceStream(string name);

        public virtual void Dispose() { }

        private static string GetString(Stream stream)
        {
            if (stream == null) return null;

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
