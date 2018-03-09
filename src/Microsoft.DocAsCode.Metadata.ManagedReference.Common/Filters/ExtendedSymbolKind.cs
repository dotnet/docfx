// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
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
}
