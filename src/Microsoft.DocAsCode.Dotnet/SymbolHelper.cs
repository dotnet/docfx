using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Microsoft.DocAsCode.Dotnet
{
    internal static class SymbolHelper
    {
        public static MetadataItem? GenerateMetadataItem(this IAssemblySymbol assembly, ExtractMetadataOptions? config = null, DotnetApiCatalogOptions? options = null, IMethodSymbol[]? extensionMethods = null)
        {
            config ??= new();
            return assembly.Accept(new SymbolVisitorAdapter(new YamlModelGenerator(), config, new(config, options ?? new()), extensionMethods));
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

        public static IEnumerable<IMethodSymbol> FindExtensionMethods(this IAssemblySymbol assembly)
        {
            if (!assembly.MightContainExtensionMethods)
                return Array.Empty<IMethodSymbol>();

            return
                from ns in assembly.GetAllNamespaces()
                from type in ns.GetTypeMembers()
                where type.MightContainExtensionMethods
                from member in type.GetMembers()
                where member.Kind is SymbolKind.Method && ((IMethodSymbol)member).IsExtensionMethod
                select (IMethodSymbol)member;
        }

        public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this IAssemblySymbol assembly)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(assembly.GlobalNamespace);
            while (stack.TryPop(out var ns))
            {
                yield return ns;
                foreach (var child in ns.GetNamespaceMembers())
                {
                    stack.Push(child);
                }
            }
        }
    }
}
