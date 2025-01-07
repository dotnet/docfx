// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet;

internal sealed partial class SpecIdHelper
{
    [GeneratedRegex(@"\B(?<!`)`\d+")]
    private static partial Regex TypeParameterRegex();

    [GeneratedRegex(@"\B``\d+")]
    private static partial Regex MethodParameterRegex();

    public static string GetSpecId(
        ISymbol symbol,
        IReadOnlyList<string> typeGenericParameters,
        IReadOnlyList<string> methodGenericParameters = null)
    {
        var id = symbol.Accept(SpecIdCoreVisitor.Instance);
        if (methodGenericParameters == null)
        {
            id = SpecMethodGenericParameter(symbol as IMethodSymbol ?? symbol.ContainingSymbol as IMethodSymbol, id);
        }
        else
        {
            id = SpecMethodGenericParameter(methodGenericParameters, id);
        }
        id = SpecTypeGenericParameter(typeGenericParameters, id);
        if (symbol is IMethodSymbol)
        {
            id = SpecExtensionMethodReceiverType(symbol as IMethodSymbol, id);
        }

        return id;
    }

    private static string SpecMethodGenericParameter(IMethodSymbol symbol, string id)
    {
        if (symbol == null)
        {
            return id;
        }
        return MethodParameterRegex().Replace(
            id,
            match =>
            {
                Debug.Assert(symbol.IsGenericMethod);
                Debug.Assert(symbol.TypeParameters.Length > int.Parse(match.Value.Substring(2)));
                return "{" + symbol.TypeParameters[int.Parse(match.Value.Substring(2))].Name + "}";
            });
    }

    private static string SpecTypeGenericParameter(IReadOnlyList<string> names, string id)
    {
        if (names == null || names.Count == 0)
        {
            return id;
        }
        return TypeParameterRegex().Replace(
            id,
            match =>
            {
                Debug.Assert(names.Count > int.Parse(match.Value.Substring(1)));
                return "{" + names[int.Parse(match.Value.Substring(1))] + "}";
            });
    }

    private static string SpecMethodGenericParameter(IReadOnlyList<string> names, string id)
    {
        if (names == null || names.Count == 0)
        {
            return id;
        }
        return MethodParameterRegex().Replace(
            id,
            match =>
            {
                Debug.Assert(names.Count > int.Parse(match.Value.Substring(2)));
                return "{" + names[int.Parse(match.Value.Substring(2))] + "}";
            });
    }

    /// <summary>
    /// spec extension method's receiver type.
    /// for below overload: M(this A), M(this A, A), AddReference applies to the first method and AddSpecReference applies to the second method might get same id without prepending receiver type.
    /// </summary>
    /// <param name="symbol">symbol</param>
    /// <param name="id">id</param>
    /// <returns>id prefixed with receiver type</returns>
    private static string SpecExtensionMethodReceiverType(IMethodSymbol symbol, string id)
    {
        if (symbol.ReducedFrom == null || symbol.ReceiverType == null)
        {
            return id;
        }

        return VisitorHelper.GetId(symbol.ReceiverType) + "." + id;
    }
}
