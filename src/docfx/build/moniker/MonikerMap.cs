// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between document and monikers
    /// </summary>
    internal class MonikerMap
    {
        private readonly HashSet<Document> _docs = new HashSet<Document>();

        private readonly ConcurrentDictionary<Document, List<string>> _documentToMonikers = new ConcurrentDictionary<Document, List<string>>();

        public bool Contains(Document doc) => _docs.Contains(doc);

        public bool TryAdd(Document doc, List<string> monikers)
        {
            if (_documentToMonikers.TryAdd(doc, monikers))
            {
                _docs.Add(doc);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(Document doc, out List<string> monikers)
        {
            return _documentToMonikers.TryGetValue(doc, out monikers);
        }
    }
}
