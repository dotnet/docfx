// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public class CachedFilterVisitor : DelegatingFilterVisitor
    {
        private readonly Dictionary<CachedKey, bool> _cache;
        private readonly Dictionary<CachedKey, bool> _attributeCache;

        public CachedFilterVisitor(IFilterVisitor inner) : base(inner)
        {
            _cache = new Dictionary<CachedKey, bool>();
            _attributeCache = new Dictionary<CachedKey, bool>();
        }

        public override bool CanVisitApi(ISymbol symbol, bool wantProtectedMember = true)
        {
            bool result;
            var key = new CachedKey(symbol, wantProtectedMember);
            if (_cache.TryGetValue(key, out result))
            {
                return result;
            }
            result = _cache[key] = Inner.CanVisitApi(symbol, wantProtectedMember);
            return result;
        }

        public override bool CanVisitAttribute(ISymbol symbol, bool wantProtectedMember = true)
        {
            bool result;
            var key = new CachedKey(symbol, wantProtectedMember);
            if (_attributeCache.TryGetValue(key, out result))
            {
                return result;
            }
            result = _attributeCache[key] = Inner.CanVisitAttribute(symbol, wantProtectedMember);
            return result;
        }

        private sealed class CachedKey : IEquatable<CachedKey>
        {
            public ISymbol Symbol { get; set; }

            public bool WantProtectedMember { get; set; }

            public CachedKey(ISymbol symbol, bool wantProtectedMember)
            {
                Symbol = symbol;
                WantProtectedMember = wantProtectedMember; 
            }

            public bool Equals(CachedKey other)
            {
                if (other == null)
                {
                    return false;
                }

                if (object.ReferenceEquals(this, other))
                {
                    return true;
                }

                return Symbol.Equals(other.Symbol) && WantProtectedMember == other.WantProtectedMember;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as CachedKey);
            }

            public override int GetHashCode()
            {
                return new { Symbol, WantProtectedMember }.GetHashCode();
            }
        }
    }
}
