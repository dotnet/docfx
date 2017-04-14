// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine;

    public sealed class ArchiveResourceCollection : ResourceCollection
    {
        private readonly object _locker = new object();
        private ZipArchive _zipped;
        private bool disposed = false;
        public override string Name { get; }
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public ArchiveResourceCollection(Stream stream, string name)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _zipped = new ZipArchive(stream);
            Name = name;
            // When Name is empty, entry is folder, ignore
            Names = _zipped.Entries.Where(s => !string.IsNullOrEmpty(s.Name)).Select(s => s.FullName);
            IsEmpty = !Names.Any();
        }

        /// <summary>
        /// TODO: This is not thread safe, only expose GetResource in interface
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Stream GetResourceStream(string name)
        {
            if (IsEmpty) return null;

            lock (_locker)
            {
                var memoryStream = new MemoryStream();
                using (var stream = GetResourceStreamCore(name))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    stream.CopyTo(memoryStream);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
        }

        public override string GetResource(string name)
        {
            lock (_locker)
            {
                using (var stream = GetResourceStreamCore(name))
                {
                    return GetString(stream);
                }
            }
        }

        private Stream GetResourceStreamCore(string name)
        {
            // zip entry is case sensitive
            // incase relative path is combined by backslash \
            return _zipped.GetEntry(StringExtension.ToNormalizedPath(name.Trim()))?.Open();
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
        private const int MaxSearchLevel = 5;
        // keep comparer to be case sensitive as to be consistent with zip entries
        private static StringComparer ResourceComparer = StringComparer.Ordinal;
        private string _directory = null;
        private readonly int _maxDepth;
        public override string Name { get; }
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public FileResourceCollection(string directory, int maxSearchLevel = MaxSearchLevel)
        {
            if (string.IsNullOrEmpty(directory)) _directory = Directory.GetCurrentDirectory();
            else _directory = directory;
            Name = _directory;
            _maxDepth = maxSearchLevel;
            var includedFiles = GetFiles(_directory, "*", maxSearchLevel);
            Names = includedFiles.Select(s => PathUtility.MakeRelativePath(_directory, s)).Where(s => s != null);

            IsEmpty = !Names.Any();
        }

        public override Stream GetResourceStream(string name)
        {
            if (IsEmpty) return null;

            // incase relative path is combined by backslash \
            if (!Names.Contains(StringExtension.ToNormalizedPath(name.Trim()), ResourceComparer)) return null;
            var filePath = Path.Combine(_directory, name);
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        private IEnumerable<string> GetFiles(string directory, string searchPattern, int searchLevel)
        {
            if (searchLevel < 1)
            {
                return Enumerable.Empty<string>();
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
        private ResourceCollection[] _collectionsInOverriddenOrder = null;
        private bool disposed = false;
        public override string Name => "Composite";
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public CompositeResourceCollectionWithOverridden(IEnumerable<ResourceCollection> collectionsInOverriddenOrder)
        {
            if (collectionsInOverriddenOrder == null || !collectionsInOverriddenOrder.Any())
            {
                IsEmpty = true;
            }
            else
            {
                _collectionsInOverriddenOrder = collectionsInOverriddenOrder.ToArray();
                Names = _collectionsInOverriddenOrder.SelectMany(s => s.Names).Distinct();
            }
        }

        public override Stream GetResourceStream(string name)
        {
            if (IsEmpty) return null;
            for (int i = _collectionsInOverriddenOrder.Length - 1; i > -1; i--)
            {
                var stream = _collectionsInOverriddenOrder[i].GetResourceStream(name);
                if (stream != null)
                {
                    Logger.LogDiagnostic($"Resource \"{name}\" is found from \"{_collectionsInOverriddenOrder[i].Name}\"");
                    return stream;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            if (_collectionsInOverriddenOrder != null)
            {
                for (int i = 0; i < _collectionsInOverriddenOrder.Length; i++)
                {
                    _collectionsInOverriddenOrder[i].Dispose();
                    _collectionsInOverriddenOrder[i] = null;
                }

                _collectionsInOverriddenOrder = null;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class EmptyResourceCollection : ResourceCollection
    {
        private static readonly IEnumerable<string> Empty = new string[0];
        public override bool IsEmpty => true;
        public override string Name => "Empty";

        public override IEnumerable<string> Names => Empty;

        public override Stream GetResourceStream(string name)
        {
            return Stream.Null;
        }
    }

    public abstract class ResourceCollection : IDisposable
    {
        public abstract string Name { get; }

        public abstract bool IsEmpty { get; }

        public abstract IEnumerable<string> Names { get; }

        public virtual string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
                return GetString(stream);
        }

        public IEnumerable<KeyValuePair<string, string>> GetResources(string selector = null)
        {
            foreach(var pair in GetResourceStreams(selector))
            {
                using (pair.Value)
                {
                    yield return new KeyValuePair<string, string>(pair.Key, GetString(pair.Value));
                }
            }
        }

        public IEnumerable<KeyValuePair<string, Stream>> GetResourceStreams(string selector = null)
        {
            Func<string, bool> filter = s =>
            {
                if (selector != null)
                {
                    var regex = new Regex(selector, RegexOptions.IgnoreCase);
                    return regex.IsMatch(s);
                }
                else
                {
                    return true;
                }
            };
            foreach (var name in Names)
            {
                if (filter(name))
                {
                    yield return new KeyValuePair<string, Stream>(name, GetResourceStream(name));
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

        protected static string GetString(Stream stream)
        {
            if (stream == null) return null;

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
