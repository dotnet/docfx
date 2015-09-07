// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class FileCollection
    {
        private readonly List<FileAndType> _files = new List<FileAndType>();

        public FileCollection(string baseDir)
        {
            BaseDir = baseDir;
        }

        public string BaseDir { get; set; }

        public void AddArticle(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.Article));
        }

        public void AddOverrides(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.Override));
        }

        public void AddToc(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.Toc));
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
