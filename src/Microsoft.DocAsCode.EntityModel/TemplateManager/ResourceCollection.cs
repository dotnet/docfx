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
        private bool disposed = false;

        public IEnumerable<string> Names { get; set; }

        public string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
                return GetString(stream);
        }

        public IEnumerable<KeyValuePair<string, string>> GetResources(string selector)
        {
            foreach(var tuple in GetResourceStreams(selector))
            {
                using (tuple.Value)
                    yield return new KeyValuePair<string, string>(tuple.Key, GetString(tuple.Value));
            }
        }

        public abstract Stream GetResourceStream(string name);

        public IEnumerable<KeyValuePair<string,Stream>> GetResourceStreams(string selector)
        {
            var regex = $"^{GlobPathHelper.GlobPatternToRegex(selector, false, true)}$";

            foreach(var name in Names)
            {
                if (new Regex(regex, RegexOptions.IgnoreCase).IsMatch(name))
                {
                    yield return new KeyValuePair<string, Stream>(name, GetResourceStream(name));
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                // Free any other managed objects here.
                Names = null;
            }

            // Free any unmangaed objects here.
            disposed = true;
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
