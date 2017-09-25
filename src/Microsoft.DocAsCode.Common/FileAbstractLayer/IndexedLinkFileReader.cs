// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class IndexedLinkFileReader : IFileReader
    {
        private readonly Dictionary<RelativePath, PathMapping> _mappings;

        public IndexedLinkFileReader(Dictionary<RelativePath, PathMapping> mappings)
        {
            _mappings = mappings;
        }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            lock (_mappings)
            {
                if (_mappings.TryGetValue(file, out PathMapping mp))
                {
                    return mp;
                }
            }
            return null;
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            lock (_mappings)
            {
                return _mappings.Keys.ToList();
            }
        }

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file)
        {
            lock (_mappings)
            {
                if (_mappings.TryGetValue(file, out var pm))
                {
                    return new[] { pm.PhysicalPath };
                }
                else
                {
                    return Enumerable.Empty<string>();
                }
            }
        }

        #endregion
    }
}
