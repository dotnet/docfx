using Microsoft.CodeAnalysis;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    internal static class SymbolHelper
    {
        public static bool IncludeSymbol(this ISymbol symbol)
        {
            if (symbol.GetDisplayAccessibility() is null)
                return false;

            return symbol.ContainingSymbol is null || IncludeSymbol(symbol.ContainingSymbol);
        }

        public static bool IsInstanceInterfaceMember(this ISymbol symbol)
        {
            return symbol.ContainingType?.TypeKind is TypeKind.Interface && !symbol.IsStatic && IsMember(symbol);
        }

        public static bool IsStaticInterfaceMember(this ISymbol symbol)
        {
            return symbol.ContainingType?.TypeKind is TypeKind.Interface && symbol.IsStatic && IsMember(symbol);
        }

        public static bool IsMember(this ISymbol symbol)
        {
            return symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event;
        }

        public static bool IsClass(this ISymbol symbol)
        {
            return symbol.Kind is SymbolKind.NamedType && symbol is INamedTypeSymbol type && type.TypeKind is TypeKind.Class;
        }

        public static bool IsEnumMember(this ISymbol symbol)
        {
            return symbol.Kind is SymbolKind.Field && symbol.ContainingType?.TypeKind is TypeKind.Enum;
        }

        public static bool IsExplicitInterfaceImplementation(this ISymbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Method => ((IMethodSymbol)symbol).ExplicitInterfaceImplementations.Length > 0,
                SymbolKind.Property => ((IPropertySymbol)symbol).ExplicitInterfaceImplementations.Length > 0,
                SymbolKind.Event => ((IEventSymbol)symbol).ExplicitInterfaceImplementations.Length > 0,
                _ => false,
            };
        }

        public static bool IsCastOperator(this ISymbol symbol)
        {
            return symbol.Kind is SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind is MethodKind.Conversion;
        }

        public static Accessibility? GetDisplayAccessibility(this ISymbol symbol)
        {
            // Hide internal or private APIs by default
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.NotApplicable => Accessibility.NotApplicable,
                Accessibility.Public => Accessibility.Public,
                Accessibility.Protected => Accessibility.Protected,
                Accessibility.ProtectedOrInternal => Accessibility.Protected,
                _ => null,
            };
        }
    }
}
