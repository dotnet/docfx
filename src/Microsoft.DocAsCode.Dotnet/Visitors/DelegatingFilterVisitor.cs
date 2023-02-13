// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using Microsoft.CodeAnalysis;

    internal class DelegatingFilterVisitor : IFilterVisitor
    {
        protected IFilterVisitor Inner { get; private set; }

        public DelegatingFilterVisitor(IFilterVisitor inner)
        {
            Inner = inner;
        }

        public bool CanVisitApi(ISymbol symbol, IFilterVisitor outer)
        {
            return CanVisitApiCore(symbol, outer ?? this);
        }

        public bool CanVisitAttribute(ISymbol symbol, IFilterVisitor outer)
        {
            return CanVisitAttributeCore(symbol, outer ?? this);
        }

        protected virtual bool CanVisitApiCore(ISymbol symbol, IFilterVisitor outer)
        {
            return Inner.CanVisitApi(symbol, outer);
        }

        protected virtual bool CanVisitAttributeCore(ISymbol symbol, IFilterVisitor outer)
        {
            return Inner.CanVisitAttribute(symbol, outer);
        }
    }
}
