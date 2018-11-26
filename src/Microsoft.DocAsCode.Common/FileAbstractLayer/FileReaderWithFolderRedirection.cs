// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FileReaderWithFolderRedirection : IFileReader
    {
        private IFileReader _inner;
        private FolderRedirectionManager _folderRedirectionManager;

        public FileReaderWithFolderRedirection(IFileReader reader, FolderRedirectionManager fdm)
        {
            _inner = reader ?? throw new ArgumentNullException(nameof(reader));
            _folderRedirectionManager = fdm ?? throw new ArgumentException(nameof(fdm));
        }

        public IEnumerable<RelativePath> EnumerateFiles() =>
            _inner.EnumerateFiles().Select(_folderRedirectionManager.GetRedirectedPath).Distinct();

        public PathMapping? FindFile(RelativePath file) =>
            _inner.FindFile(_folderRedirectionManager.GetRedirectedPath(file));

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file) =>
            _inner.GetExpectedPhysicalPath(_folderRedirectionManager.GetRedirectedPath(file));
    }
}