// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between document and monikers
    /// </summary>
    internal class MonikerMap
    {
        private readonly IReadOnlyDictionary<Document, List<string>> _documentToMonikers;

        public MonikerMap(IReadOnlyDictionary<Document, List<string>> monikerMap)
        {
            _documentToMonikers = monikerMap;
        }

        public bool TryGetValue(Document doc, out List<string> monikers)
        {
            return _documentToMonikers.TryGetValue(doc, out monikers);
        }
    }
}
