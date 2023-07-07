// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal enum ExtendedSymbolKind
{
    Assembly = 0x100,
    Namespace = 0x110,
    Type = 0x120,
    Class,
    Struct,
    Enum,
    Interface,
    Delegate,
    Member = 0x200,
    Event,
    Field,
    Method,
    Property,
}

internal static class ExtendedSymbolKindHelper
{
    public static bool Contains(this ExtendedSymbolKind kind, SymbolFilterData symbol)
    {
        ExtendedSymbolKind? k = symbol.Kind;

        if (k == null)
        {
            return false;
        }
        return (kind & k.Value) == kind;
    }
}
