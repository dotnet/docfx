﻿namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

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
