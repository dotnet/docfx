// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet;

internal sealed class SpecIdCoreVisitor : SymbolVisitor<string>
{
    public static readonly SpecIdCoreVisitor Instance = new();

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
