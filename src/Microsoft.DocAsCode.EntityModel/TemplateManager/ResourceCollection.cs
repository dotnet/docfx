// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Utility;

    public sealed class ArchiveResourceCollection : ResourceCollection
    {
        private ZipArchive _zipped = null;
        private bool disposed = false;
        public override string Name { get; }
        public override IEnumerable<string> Names { get; }

        public ArchiveResourceCollection(Stream stream, string name)
        {
            _zipped = new ZipArchive(stream);
            Name = name;
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

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            _zipped?.Dispose();
            _zipped = null;
            disposed = true;

            base.Dispose(disposing);
        }
    }

    public sealed class FileResourceCollection : ResourceCollection
    {
        // keep comparer to be case sensitive as to be consistent with zip entries
        private static StringComparer ResourceComparer = StringComparer.Ordinal;
        private string _directory = null;
        public override string Name { get; }
        public override IEnumerable<string> Names { get; }

        public FileResourceCollection(string directory, int maxSearchLevel = 3)
        {
            if (string.IsNullOrEmpty(directory)) _directory = Environment.CurrentDirectory;
            else _directory = directory;
            Name = _directory;
            Names = GetFiles(_directory, "*", maxSearchLevel).Select(s => PathUtility.MakeRelativePath(_directory, s)).Where(s => s != null);
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

        private IEnumerable<string> GetFiles(string directory, string searchPattern, int searchLevel)
        {
            if (searchLevel < 1) return Enumerable.Empty<string>();
            var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            if (searchLevel == 1) return files;
            var dirs = Directory.GetDirectories(directory);
            List<string> allFiles = new List<string>(files);
            foreach(var dir in dirs)
            {
                allFiles.AddRange(GetFiles(dir, searchPattern, searchLevel - 1));
            }
            return allFiles;
        }
    }

    public sealed class CompositeResourceCollectionWithOverridden : ResourceCollection
    {
        private ResourceCollection[] _collectionsInOverriddenOrder;
        private bool disposed = false;
        public override string Name => "Composite";
        public override IEnumerable<string> Names { get; }

        public CompositeResourceCollectionWithOverridden(ResourceCollection[] collectionsInOverriddenOrder)
        {
            if (collectionsInOverriddenOrder == null) throw new ArgumentNullException(nameof(collectionsInOverriddenOrder));
            _collectionsInOverriddenOrder = collectionsInOverriddenOrder;
            Names = _collectionsInOverriddenOrder.SelectMany(s => s.Names).Distinct();
        }

        public override Stream GetResourceStream(string name)
        {
            for (int i = _collectionsInOverriddenOrder.Length - 1; i > -1; i--)
            {
                var stream = _collectionsInOverriddenOrder[i].GetResourceStream(name);
                if (stream != null)
                {
                    Logger.LogVerbose($"Resource \"{name}\" is found from \"{_collectionsInOverriddenOrder[i].Name}\"");
                    return stream;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            for(int i = 0; i< _collectionsInOverriddenOrder.Length; i++)
            {
                _collectionsInOverriddenOrder[i].Dispose();
                _collectionsInOverriddenOrder[i] = null;
            }

            _collectionsInOverriddenOrder = null;

            base.Dispose(disposing);
        }
    }

    public abstract class ResourceCollection : IDisposable
    {
        public abstract string Name { get; }

        public abstract IEnumerable<string> Names { get; }

        public string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
                return GetString(stream);
        }

        public IEnumerable<KeyValuePair<string, string>> GetResources(string selector)
        {
            var regex = new Regex(selector, RegexOptions.IgnoreCase);
            foreach (var name in Names)
            {
                if (regex.IsMatch(name))
                {
                    using (var stream = GetResourceStream(name))
                    {
                        yield return new KeyValuePair<string, string>(name, GetString(stream));
                    }
                }
            }
        }

        public abstract Stream GetResourceStream(string name);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Override Object.Finalize by defining a destructor
        /// </summary>
        ~ResourceCollection()
        {
            Dispose(false);
        }

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
