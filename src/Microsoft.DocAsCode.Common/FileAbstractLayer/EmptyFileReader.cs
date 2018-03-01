// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class EmptyFileReader : IFileReader
    {
        public static readonly EmptyFileReader Instance = new EmptyFileReader();

        private EmptyFileReader()
        {
        }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file) => null;

        public IEnumerable<RelativePath> EnumerateFiles() => Enumerable.Empty<RelativePath>();

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file) =>
            Enumerable.Empty<string>();

        #endregion
    }
}
