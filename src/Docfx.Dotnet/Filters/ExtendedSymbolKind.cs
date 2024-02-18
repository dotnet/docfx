// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

[Flags]
internal enum ExtendedSymbolKind
{
#pragma warning disable format
    Assembly  = 1 << 1,
    Namespace = 1 << 2,
    // Type
    Class     = 1 << 3,
    Struct    = 1 << 4,
    Enum      = 1 << 5,
    Interface = 1 << 6,
    Delegate  = 1 << 7,
    // Member
    Event     = 1 << 8,
    Field     = 1 << 9,
    Method    = 1 << 10,
    Property  = 1 << 11,
#pragma warning restore format

    Type = Class | Struct | Enum | Interface | Delegate,
    Member = Event | Field | Method | Property,
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
        return (kind & k.Value) > 0;
    }
}
