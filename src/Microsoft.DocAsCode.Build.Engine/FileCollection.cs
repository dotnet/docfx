// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class FileCollection
    {
        private readonly List<FileAndType> _files = new List<FileAndType>();

        public int Count => _files.Count;

        public FileCollection(string defaultBaseDir)
        {
            DefaultBaseDir = defaultBaseDir ?? string.Empty;
        }

        public FileCollection(FileCollection collection) : this(collection.DefaultBaseDir)
        {
            _files = new List<FileAndType>(collection._files);
        }

        public string DefaultBaseDir { get; set; }

        public void Add(DocumentType type, IEnumerable<string> files, string sourceDir = null, string destinationDir = null)
        {
            Add(type, null, files, sourceDir, destinationDir);
        }

        public void Add(DocumentType type, string baseDir, IEnumerable<string> files, string sourceDir = null, string destinationDir = null)
        {
            var rootedBaseDir = Path.Combine(DefaultBaseDir, baseDir ?? string.Empty);
            if (sourceDir != null)
            {
                if (sourceDir.StartsWith(rootedBaseDir))
                {
                    sourceDir = sourceDir.Substring(rootedBaseDir.Length).TrimStart('/', '\\');
                }
                else
                {
                    throw new ArgumentException("SourceDir must start with BaseDir, or relative path.", nameof(sourceDir));
                }
            }
            if (destinationDir != null)
            {
                if (destinationDir.StartsWith(rootedBaseDir))
                {
                    destinationDir = sourceDir.Substring(rootedBaseDir.Length).TrimStart('/', '\\');
                }
            }
            if (!string.IsNullOrEmpty(sourceDir) && !sourceDir.EndsWith("/"))
            {
                sourceDir += "/";
            }
            if (!string.IsNullOrEmpty(destinationDir) && !destinationDir.EndsWith("/"))
            {
                destinationDir += "/";
            }
            _files.AddRange(from f in files
                            select new FileAndType(rootedBaseDir, ToRelative(f, rootedBaseDir), type, sourceDir, destinationDir));
        }

        public void RemoveAll(Predicate<FileAndType> match)
        {
            _files.RemoveAll(match);
        }

        private string ToRelative(string file, string rootedBaseDir)
        {
            return PathUtility.MakeRelativePath(rootedBaseDir, file);
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
