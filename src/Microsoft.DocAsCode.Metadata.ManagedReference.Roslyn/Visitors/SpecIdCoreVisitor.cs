// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    internal sealed class SpecIdCoreVisitor : SymbolVisitor<string>
    {
        public static readonly SpecIdCoreVisitor Instance = new SpecIdCoreVisitor();

        private SpecIdCoreVisitor()
        {
        }

        public override string DefaultVisit(ISymbol symbol)
        {
            return VisitorHelper.GetId(symbol);
        }

        public override string VisitPointerType(IPointerTypeSymbol symbol)
        {
            return symbol.PointedAtType.Accept(this) + "*";
        }

        public override string VisitArrayType(IArrayTypeSymbol symbol)
        {
            if (symbol.Rank == 1)
            {
                return symbol.ElementType.Accept(this) + "[]";
            }
            else
            {
                return symbol.ElementType.Accept(this) + "[" + new string(',', symbol.Rank - 1) + "]";
            }
        }

        public override string VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            return "{" + symbol.Name + "}";
        }
    }
}
