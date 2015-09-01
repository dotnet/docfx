// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Collections.Generic;
    using System.Linq;

    public class FileCollection
    {
        private readonly List<FileAndType> _files = new List<FileAndType>();

        public FileCollection(string baseDir)
        {
            BaseDir = baseDir;
        }

        public string BaseDir { get; set; }

        public void AddApis(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.ApiDocument));
        }

        public void AddOverrides(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.OverrideDocument));
        }

        public void AddConceptual(IEnumerable<string> files)
        {
            _files.AddRange(from f in files
                            select new FileAndType(BaseDir, f, DocumentType.ConceptualDocument));
        }

        public IEnumerable<FileAndType> EnumerateFiles()
        {
            return _files.Distinct();
        }
    }
}
