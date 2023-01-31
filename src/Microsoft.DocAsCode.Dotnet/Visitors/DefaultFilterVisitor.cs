// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.CodeAnalysis;

    public class DefaultFilterVisitor : IFilterVisitor
    {
        public bool CanVisitApi(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, (outer ?? this).CanVisitApi, wantProtectedMember, outer ?? this);
        }

        public bool CanVisitAttribute(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, (outer ?? this).CanVisitAttribute, wantProtectedMember, outer ?? this);
        }

        private static bool CanVisitCore(ISymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtectedMember, IFilterVisitor outer)
        {
            // check parent visibility
            var current = symbol;
            var parent = symbol.ContainingSymbol;
            while (!(current is INamespaceSymbol) && parent != null)
            {
                if (!visitFunc(parent, wantProtectedMember, outer))
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
                return CanVisitCore(methodSymbol, visitFunc, wantProtectedMember, outer);
            }

            if (symbol is IPropertySymbol propertySymbol)
            {
                return CanVisitCore(propertySymbol, visitFunc, wantProtectedMember, outer);
            }

            if (symbol is IEventSymbol eventSymbol)
            {
                return CanVisitCore(eventSymbol, visitFunc, wantProtectedMember, outer);
            }

            if (symbol is IFieldSymbol fieldSymbol)
            {
                return CanVisitCore(fieldSymbol, visitFunc, wantProtectedMember, outer);
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return CanVisitCore(namedTypeSymbol, visitFunc, wantProtectedMember, outer);
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
                        return visitFunc(((IArrayTypeSymbol)ts).ElementType, wantProtectedMember, outer);
                    case TypeKind.Pointer:
                        return visitFunc(((IPointerTypeSymbol)ts).PointedAtType, wantProtectedMember, outer);
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

        private static bool CanVisitCore(INamedTypeSymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtectedMember, IFilterVisitor outer)
        {
            if (symbol.ContainingType != null)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        return visitFunc(symbol.ContainingType, wantProtectedMember, outer);
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        return wantProtectedMember && visitFunc(symbol.ContainingType, wantProtectedMember, outer);
                    default:
                        return false;
                }
            }
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool CanVisitCore(IMethodSymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtectedMember, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], false, outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IPropertySymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtectedMember, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], false, outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IEventSymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtectedMember, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (visitFunc(symbol.ExplicitInterfaceImplementations[i], false, outer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IFieldSymbol symbol, Func<ISymbol, bool, IFilterVisitor, bool> visitFunc, bool wantProtected, IFilterVisitor outer)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtected;
                default:
                    break;
            }
            return false;
        }
    }
}
