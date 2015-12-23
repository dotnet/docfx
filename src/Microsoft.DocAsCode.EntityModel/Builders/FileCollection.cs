// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
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
                DefaultBaseDir = Environment.CurrentDirectory;
            }
            else
            {
                DefaultBaseDir = Path.Combine(Environment.CurrentDirectory, defaultBaseDir);
            }
        }

        public string DefaultBaseDir { get; set; }

        public void Add(DocumentType type, IEnumerable<string> files, Func<string, string> pathRewriter = null)
        {
            _files.AddRange(from f in files
                            select new FileAndType(DefaultBaseDir, ToRelative(f, DefaultBaseDir), type, pathRewriter));
        }

        public void Add(DocumentType type, string baseDir, IEnumerable<string> files, Func<string, string> pathRewriter = null)
        {
            var rootedBaseDir = Path.Combine(Environment.CurrentDirectory, baseDir ?? string.Empty);
            _files.AddRange(from f in files
                            select new FileAndType(rootedBaseDir, ToRelative(f, rootedBaseDir), type, pathRewriter));
        }

        private string ToRelative(string file, string rootedBaseDir)
        {
            if (!Path.IsPathRooted(file))
            {
                return file;
            }
            return PathUtility.MakeRelativePath(rootedBaseDir, file);
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
