// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerMapBuilder
    {
        private readonly ConcurrentDictionary<Document, List<string>> _documentToMonikers = new ConcurrentDictionary<Document, List<string>>();

        public void Add(Document file, List<string> monikers)
        {
            _documentToMonikers.TryAdd(file, monikers);
        }

        public MonikerMap Build()
        {
            return new MonikerMap(_documentToMonikers);
        }
    }
}
