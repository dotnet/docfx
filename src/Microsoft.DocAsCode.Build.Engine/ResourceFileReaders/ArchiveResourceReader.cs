// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public sealed class ArchiveResourceReader : ResourceFileReader
    {
        private readonly object _locker = new object();
        private ZipArchive _zipped;
        private bool disposed = false;

        public override string Name { get; }
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public ArchiveResourceReader(Stream stream, string name)
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
}
