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

        public virtual bool CanVisitApi(ISymbol symbol, bool wantProtectedMember = true)
        {
            return Inner.CanVisitApi(symbol, wantProtectedMember);
        }

        public virtual bool CanVisitAttribute(ISymbol symbol, bool wantProtectedMember = true)
        {
            return Inner.CanVisitAttribute(symbol, wantProtectedMember);
        }
    }
}
