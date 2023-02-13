// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    internal class CachedFilterVisitor : DelegatingFilterVisitor
    {
        private readonly Dictionary<ISymbol, bool> _cache;
        private readonly Dictionary<ISymbol, bool> _attributeCache;

        public CachedFilterVisitor(IFilterVisitor inner) : base(inner)
        {
            _cache = new Dictionary<ISymbol, bool>();
            _attributeCache = new Dictionary<ISymbol, bool>();
        }

        protected override bool CanVisitApiCore(ISymbol symbol, IFilterVisitor outer)
        {
            if (_cache.TryGetValue(symbol, out bool result))
            {
                return result;
            }
            result = _cache[symbol] = Inner.CanVisitApi(symbol, outer);
            return result;
        }

        protected override bool CanVisitAttributeCore(ISymbol symbol, IFilterVisitor outer)
        {
            if (_attributeCache.TryGetValue(symbol, out bool result))
            {
                return result;
            }
            result = _attributeCache[symbol] = Inner.CanVisitAttribute(symbol, outer);
            return result;
        }
    }
}
