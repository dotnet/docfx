// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

internal static class SymbolHelper
{
    public static MetadataItem? GenerateMetadataItem(this IAssemblySymbol assembly, Compilation compilation, ExtractMetadataConfig? config = null, DotnetApiOptions? options = null, IMethodSymbol[]? extensionMethods = null)
    {
        config ??= new();
        return assembly.Accept(new SymbolVisitorAdapter(compilation, new(compilation, MemberLayout.SamePage, new(new[] { assembly }, SymbolEqualityComparer.Default)), config, new(config, options ?? new()), extensionMethods));
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
        return symbol.Kind is SymbolKind.NamedType && symbol is INamedTypeSymbol { TypeKind: TypeKind.Class };
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

    public static bool HasOverloads(this ISymbol symbol)
    {
        return symbol.Kind is SymbolKind.Method && symbol.ContainingType.GetMembers().Any(
            m => m.Kind is SymbolKind.Method && !ReferenceEquals(m, symbol) && m.Name == symbol.Name);
    }

    public static bool IsConstructor(IMethodSymbol method)
    {
        return method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor;
    }

    public static bool IsMethod(IMethodSymbol method)
    {
        return method.MethodKind
            is MethodKind.AnonymousFunction
            or MethodKind.DelegateInvoke
            or MethodKind.Destructor
            or MethodKind.ExplicitInterfaceImplementation
            or MethodKind.Ordinary
            or MethodKind.ReducedExtension
            or MethodKind.DeclareMethod;
    }

    public static bool IsOperator(IMethodSymbol method)
    {
        return method.MethodKind is MethodKind.BuiltinOperator or MethodKind.UserDefinedOperator or MethodKind.Conversion;
    }

    public static IEnumerable<IMethodSymbol> FindExtensionMethods(this IAssemblySymbol assembly, SymbolFilter filter)
    {
        if (!assembly.MightContainExtensionMethods)
            return Array.Empty<IMethodSymbol>();

        return
            from ns in assembly.GetAllNamespaces(filter)
            from type in ns.GetTypeMembers()
            where type.MightContainExtensionMethods
            from member in type.GetMembers()
            where member.Kind is SymbolKind.Method && ((IMethodSymbol)member).IsExtensionMethod
            where filter.IncludeApi(member)
            select (IMethodSymbol)member;
    }

    public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this IAssemblySymbol assembly, SymbolFilter filter)
    {
        var stack = new Stack<INamespaceSymbol>();
        stack.Push(assembly.GlobalNamespace);
        while (stack.TryPop(out var item))
        {
            if (!filter.IncludeApi(item))
                continue;

            yield return item;
            foreach (var child in item.GetNamespaceMembers())
            {
                stack.Push(child);
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this IAssemblySymbol assembly, SymbolFilter filter)
    {
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(assembly.GlobalNamespace);

        while (stack.TryPop(out var item))
        {
            if (!filter.IncludeApi(item))
                continue;

            if (item is INamedTypeSymbol type)
            {
                yield return type;

                foreach (var child in type.GetTypeMembers())
                    stack.Push(child);
            }
            else if (item is INamespaceSymbol ns)
            {
                foreach (var child in ns.GetNamespaceMembers())
                    stack.Push(child);

                foreach (var child in ns.GetTypeMembers())
                    stack.Push(child);
            }
        }
    }

    public static IEnumerable<ISymbol> GetInheritedMembers(this INamedTypeSymbol symbol, SymbolFilter filter)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.SpecialType is SpecialType.System_ValueType)
                continue;

            foreach (var m in from m in type.GetMembers()
                              where m is not INamedTypeSymbol
                              where filter.IncludeApi(m)
                              where m.DeclaredAccessibility is Accessibility.Public || !(symbol.IsSealed || symbol.TypeKind is TypeKind.Struct)
                              where IsInheritable(m)
                              select m)
            {
                yield return m;
            }
        }

        static bool IsInheritable(ISymbol memberSymbol)
        {
            if (memberSymbol is IMethodSymbol { } method)
            {
                return method.MethodKind switch
                {
                    MethodKind.ExplicitInterfaceImplementation or MethodKind.DeclareMethod or MethodKind.Ordinary => true,
                    _ => false,
                };
            }
            return true;
        }
    }

    public static bool TryGetExplicitInterfaceImplementations(ISymbol symbol, [MaybeNullWhen(false)] out IEnumerable<ISymbol> eiis)
    {
        eiis = symbol.Kind switch
        {
            SymbolKind.Method => ((IMethodSymbol)symbol).ExplicitInterfaceImplementations,
            SymbolKind.Property => ((IPropertySymbol)symbol).ExplicitInterfaceImplementations,
            SymbolKind.Event => ((IEventSymbol)symbol).ExplicitInterfaceImplementations,
            _ => null,
        };

        return eiis != null;
    }
}
