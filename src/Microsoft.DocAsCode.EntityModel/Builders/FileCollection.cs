// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class FileCollection
    {
        private readonly List<FileAndType> _files = new List<FileAndType>();

        public FileCollection(string defaultBaseDir)
        {
            DefaultBaseDir = defaultBaseDir;
        }

        public string DefaultBaseDir { get; set; }

        public void Add(DocumentType type, IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(DefaultBaseDir, f, type));
        }

        public void Add(DocumentType type, string baseDir, IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(baseDir, f, type));
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
