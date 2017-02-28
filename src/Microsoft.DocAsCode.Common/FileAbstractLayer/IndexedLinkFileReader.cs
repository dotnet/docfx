// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class IndexedLinkFileReader : IFileReader
    {
        public IndexedLinkFileReader(Dictionary<RelativePath, PathMapping> mappings)
        {
            Mappings = mappings;
        }

        public Dictionary<RelativePath, PathMapping> Mappings { get; }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            PathMapping mp;
            lock (Mappings)
            {
                if (Mappings.TryGetValue(file, out mp))
                {
                    return mp;
                }
            }
            return null;
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            lock (Mappings)
            {
                return Mappings.Keys.ToList();
            }
        }

        #endregion
    }
}
