// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public sealed class XRefArchive : IDisposable
    {
        #region Consts / Fields
        public const string MajorFileName = "xrefmap.yml";

        private readonly object _syncRoot = new object();
        private readonly ZipArchive _archive;
        private readonly List<string> _entries;
        #endregion

        #region Ctors

        private XRefArchive(ZipArchive archive, List<string> entries)
        {
            _archive = archive;
            _entries = entries;
        }

        #endregion

        #region Public Members

        public static XRefArchive Create(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            var directory = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var fs = File.Create(file);
            try
            {
                return new XRefArchive(
                    new ZipArchive(fs, ZipArchiveMode.Create),
                    new List<string>());
            }
            catch (Exception)
            {
                fs.Close();
                throw;
            }
        }

        public static XRefArchive Open(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"File not found: {file}", file);
            }
            var fs = File.Open(file, FileMode.Open);
            try
            {
                var zip = new ZipArchive(fs, ZipArchiveMode.Update);
                var list = (from entry in zip.Entries
                            select entry.FullName).ToList();
                list.Sort(StringComparer.OrdinalIgnoreCase);
                return new XRefArchive(zip, list);
            }
            catch (Exception)
            {
                fs.Close();
                throw;
            }
        }

        public string CreateMajor(XRefMap map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            if (HasEntryCore(MajorFileName))
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
            if (names != null)
            {
                foreach (var name in names)
                {
                    var entryName = NormalizeName(name);
                    if (entryName != null &&
                        HasEntryCore(entryName))
                    {
                        return CreateCore(entryName, map);
                    }
                }
            }
            while (true)
            {
                var entryName = Guid.NewGuid().ToString() + ".yml";
                if (HasEntryCore(entryName))
                {
                    return CreateCore(entryName, map);
                }
            }
        }

        public XRefMap GetMajor() => Get(MajorFileName);

        public XRefMap Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var entryName = GetEntry(name);
            if (entryName == null)
            {
                throw new InvalidOperationException($"Entry {name} not found.");
            }
            return OpenCore(name);
        }

        public void UpdateMajor(XRefMap map) => Update(MajorFileName, map);

        public void Update(string name, XRefMap map)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            var entryName = GetEntry(name);
            if (entryName == null)
            {
                throw new InvalidOperationException($"Entry {name} not found.");
            }
            UpdateCore(name, map);
        }

        public void DeleteMajor() => Delete(MajorFileName);

        public void Delete(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var index = IndexOfEntry(name);
            if (index < 0)
            {
                throw new InvalidOperationException($"Entry {name} not found.");
            }
            DeleteCore(index);
        }

        public bool HasEntry(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return HasEntryCore(name);
        }

        public ImmutableList<string> Entries => _entries.ToImmutableList();

        #endregion

        #region Private Methods

        private bool HasEntryCore(string name) => IndexOfEntry(name) >= 0;

        private string GetEntry(string name)
        {
            var index = IndexOfEntry(name);
            if (index >= 0)
            {
                return _entries[index];
            }
            return null;
        }

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

        private XRefMap OpenCore(string name)
        {
            var entry = _archive.GetEntry(name);
            using (var sr = new StreamReader(entry.Open()))
            {
                return YamlUtility.Deserialize<XRefMap>(sr);
            }
        }

        private void UpdateCore(string name, XRefMap map)
        {
            var entry = _archive.GetEntry(name);
            entry.Delete();
            entry = _archive.CreateEntry(name);
            using (var sw = new StreamWriter(entry.Open()))
            {
                YamlUtility.Serialize(sw, map);
            }
        }

        private void DeleteCore(int index)
        {
            var entry = _archive.GetEntry(_entries[index]);
            entry.Delete();
            _entries.RemoveAt(index);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _archive.Dispose();
        }

        #endregion
    }
}
