// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    public class DelegatingFilterVisitor : IFilterVisitor
    {
        protected IFilterVisitor Inner { get; private set; }

        public DelegatingFilterVisitor(IFilterVisitor inner)
        {
            Inner = inner;
        }

        public bool CanVisitApi(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            return CanVisitApiCore(symbol, wantProtectedMember, outer ?? this);
        }

        public bool CanVisitAttribute(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            return CanVisitAttributeCore(symbol, wantProtectedMember, outer ?? this);
        }

        protected virtual bool CanVisitApiCore(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            return Inner.CanVisitApi(symbol, wantProtectedMember, outer);
        }

        protected virtual bool CanVisitAttributeCore(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            return Inner.CanVisitAttribute(symbol, wantProtectedMember, outer);
        }
    }
}
