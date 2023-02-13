// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using System;

    using Microsoft.CodeAnalysis;

    internal class DefaultFilterVisitor : IFilterVisitor
    {
        public bool CanVisitApi(ISymbol symbol, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, (outer ?? this).CanVisitApi, outer ?? this);
        }

        public bool CanVisitAttribute(ISymbol symbol, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, (outer ?? this).CanVisitAttribute, outer ?? this);
        }

        private static bool CanVisitCore(ISymbol symbol, Func<ISymbol, IFilterVisitor, bool> visitFunc, IFilterVisitor outer)
        {
            // check parent visibility
            var current = symbol;
            var parent = symbol.ContainingSymbol;
            while (!(current is INamespaceSymbol) && parent != null)
            {
                if (!visitFunc(parent, outer))
                {
                    return false;
                }

                current = parent;
                parent = parent.ContainingSymbol;
            }

            if (symbol.DeclaredAccessibility == Accessibility.NotApplicable)
            {
                return true;
            }

            if (!(symbol is INamespaceSymbol) && symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            if (symbol is IMethodSymbol methodSymbol)
            {
                return CanVisitMethod(methodSymbol, visitFunc, outer);
            }

            if (symbol is IPropertySymbol propertySymbol)
            {
                return CanVisitProperty(propertySymbol, visitFunc, outer);
            }

            if (symbol is IEventSymbol eventSymbol)
            {
                return CanVisitEvent(eventSymbol, visitFunc, outer);
            }

            if (symbol is IFieldSymbol fieldSymbol)
            {
                return CanVisitField(fieldSymbol);
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return CanVisitNamedType(namedTypeSymbol, visitFunc, outer);
            }

            if (symbol is ITypeSymbol ts)
            {
                switch (ts.TypeKind)
                {
                    case TypeKind.Dynamic:
                    case TypeKind.TypeParameter:
                        return true;
                    case TypeKind.Unknown:
                    case TypeKind.Error:
                        return false;
                    case TypeKind.Array:
                        return visitFunc(((IArrayTypeSymbol)ts).ElementType, outer);
                    case TypeKind.Pointer:
                        return visitFunc(((IPointerTypeSymbol)ts).PointedAtType, outer);
                    default:
                        break;
                }
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            return true;
        }

        private static bool CanVisitNamedType(INamedTypeSymbol symbol, Func<ISymbol, IFilterVisitor, bool> visitFunc, IFilterVisitor outer)
        {
            if (symbol.ContainingType != null)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        return visitFunc(symbol.ContainingType, outer);
                    default:
                        return false;
                }
            }
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool CanVisitMethod(IMethodSymbol symbol, Func<ISymbol, IFilterVisitor, bool> visitFunc, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitProperty(IPropertySymbol symbol, Func<ISymbol, IFilterVisitor, bool> visitFunc, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitEvent(IEventSymbol symbol, Func<ISymbol, IFilterVisitor, bool> visitFunc, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitField(IFieldSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;
                default:
                    break;
            }
            return false;
        }
    }
}
