// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between document and monikers
    /// </summary>
    internal class MonikerMap
    {
        private readonly Dictionary<Document, List<string>> _documentToMonikers = new Dictionary<Document, List<string>>();

        public MonikerMap(ConcurrentDictionary<Document, List<string>> monikerMap)
        {
            _documentToMonikers = monikerMap.ToDictionary(item => item.Key, item => item.Value);
        }

        public bool TryGetValue(Document doc, out List<string> monikers)
        {
            return _documentToMonikers.TryGetValue(doc, out monikers);
        }
    }
}
