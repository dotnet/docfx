// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.Common;

    public sealed class XRefArchive : IDisposable
    {
        public const string MajorFileName = "xrefmap.yml";

        private readonly object _syncRoot = new object();
        private readonly ZipArchive _archive;
        private readonly List<string> _entries;

        public XRefArchive(string file)
        {
            _archive = new ZipArchive(File.Create(file), ZipArchiveMode.Create);
            _entries = new List<string>();
        }

        public string CreateMajor(XRefMap map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            if (HasEntry(MajorFileName))
            {
                throw new InvalidOperationException("Major entry existed.");
            }
            return CreateCore(MajorFileName, map);
        }

        public string CreateMinor(XRefMap map, IEnumerable<string> names)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            foreach (var name in names)
            {
                var entryName = NormalizeName(name);
                if (entryName != null &&
                    HasEntry(entryName))
                {
                    return CreateCore(entryName, map);
                }
            }
            while (true)
            {
                var entryName = Guid.NewGuid().ToString() + ".yml";
                if (HasEntry(entryName))
                {
                    return CreateCore(entryName, map);
                }
            }
        }

        private bool HasEntry(string name) => IndexOfEntry(name) >= 0;

        private int IndexOfEntry(string name) =>
            _entries.BinarySearch(name, StringComparer.OrdinalIgnoreCase);

        private ZipArchiveEntry CreateEntry(string name)
        {
            var index = IndexOfEntry(name);
            if (index < 0)
            {
                _entries.Insert(~index, name);
            }
            return _archive.CreateEntry(name);
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return null;
            }
            var exName = Path.GetExtension(name);
            if (!".yml".Equals(exName, StringComparison.OrdinalIgnoreCase) ||
                !".yaml".Equals(exName, StringComparison.OrdinalIgnoreCase))
            {
                name += ".yml";
            }
            return name;
        }

        private string CreateCore(string name, XRefMap map)
        {
            var entry = CreateEntry(name);
            using (var sw = new StreamWriter(entry.Open()))
            {
                YamlUtility.Serialize(sw, map);
            }
            return name;
        }

        public void Dispose()
        {
            _archive.Dispose();
        }
    }
}
