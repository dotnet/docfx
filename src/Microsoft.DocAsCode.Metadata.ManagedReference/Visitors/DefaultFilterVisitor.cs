// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.CodeAnalysis;

    public class DefaultFilterVisitor : IFilterVisitor
    {
        public bool CanVisitApi(ISymbol symbol, bool wantProtectedMember = true)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, wantProtectedMember);
        }

        public bool CanVisitAttribute(ISymbol symbol, bool wantProtectedMember = true)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            return CanVisitCore(symbol, wantProtectedMember);
        }

        private static bool CanVisitCore(ISymbol symbol, bool wantProtectedMember = true)
        {
            if (symbol.DeclaredAccessibility == Accessibility.NotApplicable)
            {
                return true;
            }

            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                return CanVisitCore(methodSymbol, wantProtectedMember);
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return CanVisitCore(propertySymbol, wantProtectedMember);
            }

            var eventSymbol = symbol as IEventSymbol;
            if (eventSymbol != null)
            {
                return CanVisitCore(eventSymbol, wantProtectedMember);
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return CanVisitCore(fieldSymbol, wantProtectedMember);
            }

            var typeSymbol = symbol as INamedTypeSymbol;
            if (typeSymbol != null)
            {
                return CanVisitCore(typeSymbol, wantProtectedMember);
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            return true;
        }

        private static bool CanVisitCore(INamedTypeSymbol symbol, bool wantProtectedMember)
        {
            if (symbol.ContainingType != null)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        return CanVisitCore((ISymbol)symbol.ContainingType, wantProtectedMember);
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        return wantProtectedMember && CanVisitCore((ISymbol)symbol.ContainingType, wantProtectedMember);
                    default:
                        return false;
                }
            }
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool CanVisitCore(IMethodSymbol symbol, bool wantProtectedMember)
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
                    if (CanVisitCore(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IPropertySymbol symbol, bool wantProtectedMember)
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
                    if (CanVisitCore(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IEventSymbol symbol, bool wantProtectedMember)
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
                    if (CanVisitCore(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IFieldSymbol symbol, bool wantProtected)
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
