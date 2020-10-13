// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class LoadedExternalXrefMap
    {
        private readonly IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> _externalXrefMap;

        private readonly IReadOnlyList<Lazy<ExternalXref>> _externalXref;

        public LoadedExternalXrefMap(
            (IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> externalXrefMap, IReadOnlyList<Lazy<ExternalXref>> externalXref) input)
        {
            _externalXrefMap = input.externalXrefMap;
            _externalXref = input.externalXref;
        }

        public IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> GetExternalXrefMap()
        {
            return _externalXrefMap;
        }

        public IReadOnlyList<Lazy<ExternalXref>> GetExternalXref()
        {
            return _externalXref;
        }
    }
}
