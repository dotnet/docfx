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
        private FolderRedirectionManager _folderRedirection;

        public FileReaderWithFolderRedirection(IFileReader reader, FolderRedirectionManager folderRedirection)
        {
            _inner = reader ?? throw new ArgumentNullException(nameof(reader));
            _folderRedirection = folderRedirection ?? throw new ArgumentException(nameof(folderRedirection));
        }

        public IEnumerable<RelativePath> EnumerateFiles() =>
            _inner.EnumerateFiles().Select(_folderRedirection.GetRedirectedPath).Distinct();

        public PathMapping? FindFile(RelativePath file) =>
            _inner.FindFile(_folderRedirection.GetRedirectedPath(file));

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file) =>
            _inner.GetExpectedPhysicalPath(_folderRedirection.GetRedirectedPath(file));
    }
}