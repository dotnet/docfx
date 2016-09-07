// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class FileCollection
    {
        private readonly List<FileAndType> _files = new List<FileAndType>();

        public int Count => _files.Count;

        public FileCollection(string defaultBaseDir)
        {
            if (string.IsNullOrEmpty(defaultBaseDir))
            {
                DefaultBaseDir = Directory.GetCurrentDirectory();
            }
            else
            {
                DefaultBaseDir = Path.Combine(Directory.GetCurrentDirectory(), defaultBaseDir);
            }
        }

        public FileCollection(FileCollection collection) : this(collection.DefaultBaseDir)
        {
            _files = new List<FileAndType>(collection._files);
        }

        public string DefaultBaseDir { get; set; }

        public void Add(DocumentType type, IEnumerable<string> files, string sourceDir = null, string destinationDir = null, string pathRewriteBaseDir = null)
        {
            _files.AddRange(from f in files
                            select new FileAndType(DefaultBaseDir, ToRelative(f, DefaultBaseDir), type, sourceDir, destinationDir, pathRewriteBaseDir));
        }

        public void Add(DocumentType type, string baseDir, IEnumerable<string> files, string sourceDir = null, string destinationDir = null, string pathRewriteBaseDir = null)
        {
            var rootedBaseDir = Path.Combine(Directory.GetCurrentDirectory(), baseDir ?? string.Empty);
            _files.AddRange(from f in files
                            select new FileAndType(rootedBaseDir, ToRelative(f, rootedBaseDir), type, sourceDir, destinationDir, pathRewriteBaseDir));
        }

        public void RemoveAll(Predicate<FileAndType> match)
        {
            _files.RemoveAll(match);
        }

        private string ToRelative(string file, string rootedBaseDir)
        {
            if (!Path.IsPathRooted(file))
            {
                return file;
            }
            var result = PathUtility.MakeRelativePath(rootedBaseDir, file);
            if (Path.IsPathRooted(result))
            {
                throw new ArgumentException($"Cannot get relative path for {file} from {rootedBaseDir}.", nameof(file));
            }
            return result;
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
