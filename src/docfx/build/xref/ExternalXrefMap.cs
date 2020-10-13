// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefMap
    {
        private readonly IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> _externalXrefMap;

        private readonly IEnumerable<ExternalXref> _externalXref;

        public ExternalXrefMap(IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> externalXrefMap, IEnumerable<ExternalXref> externalXref)
        {
            _externalXrefMap = externalXrefMap;
            _externalXref = externalXref;
        }

        public bool ExternalXrefMapTryGetValue(string uid, out ExternalXrefSpec? spec)
        {
            var result = _externalXrefMap.TryGetValue(uid, out var lazySpec);
            spec = lazySpec?.Value;
            return result;
        }

        public IEnumerable<ExternalXref> GetExternalXref()
        {
            return _externalXref;
        }
    }
}
