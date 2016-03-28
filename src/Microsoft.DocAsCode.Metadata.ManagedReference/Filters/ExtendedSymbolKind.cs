// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    public enum ExtendedSymbolKind
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

    public static class ExtendedSymbolKindHelper
    {
        public static bool Contains(this ExtendedSymbolKind kind, ISymbol symbol)
        {
            ExtendedSymbolKind? k = GetExtendedSymbolKindFromSymbol(symbol);

            if (k == null)
            {
                return false;
            }
            return (kind & k.Value) == kind;
        }

        private static ExtendedSymbolKind? GetExtendedSymbolKindFromSymbol(ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    return ExtendedSymbolKind.Assembly;
                case SymbolKind.Namespace:
                    return ExtendedSymbolKind.Namespace;
                case SymbolKind.Event:
                    return ExtendedSymbolKind.Event;
                case SymbolKind.Field:
                    return ExtendedSymbolKind.Field;
                case SymbolKind.Method:
                    return ExtendedSymbolKind.Method;
                case SymbolKind.Property:
                    return ExtendedSymbolKind.Property;
                case SymbolKind.NamedType:
                    return GetExtendedSymbolKindFromINamedTypeSymbol(symbol as INamedTypeSymbol);
                default:
                    return null;
            }
        }

        private static ExtendedSymbolKind? GetExtendedSymbolKindFromINamedTypeSymbol(INamedTypeSymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                    return ExtendedSymbolKind.Class;
                case TypeKind.Struct:
                    return ExtendedSymbolKind.Struct;
                case TypeKind.Delegate:
                    return ExtendedSymbolKind.Delegate;
                case TypeKind.Enum:
                    return ExtendedSymbolKind.Enum;
                case TypeKind.Interface:
                    return ExtendedSymbolKind.Interface;
                default:
                    return null;
            }
        }
    }
}
