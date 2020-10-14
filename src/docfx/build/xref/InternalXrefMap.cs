// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefMap
    {
        private readonly IReadOnlyDictionary<string, InternalXrefSpec[]> _internalXrefMap;

        private readonly IEnumerable<(SourceInfo<string>, string)> _xrefTypes;

        public InternalXrefMap(IReadOnlyDictionary<string, InternalXrefSpec[]> internalXrefMap, IEnumerable<(SourceInfo<string>, string)> xrefTypes)
        {
            _internalXrefMap = internalXrefMap;
            _xrefTypes = xrefTypes;
        }

        public IEnumerable<InternalXrefSpec[]> GetInternalXrefValues()
        {
            return _internalXrefMap.Values;
        }

        public bool InternalXrefMapContainsKey(string key)
        {
            return _internalXrefMap.ContainsKey(key);
        }

        public IReadOnlyDictionary<string, InternalXrefSpec[]> GetInternalXrefMap()
        {
            return _internalXrefMap;
        }

        public IEnumerable<(SourceInfo<string>, string)> GetXrefTypes()
        {
            return _xrefTypes;
        }
    }
}
