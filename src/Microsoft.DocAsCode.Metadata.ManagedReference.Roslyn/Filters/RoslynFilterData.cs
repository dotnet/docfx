// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    internal class RoslynFilterData
    {
        public static SymbolFilterData GetSymbolFilterData(ISymbol symbol)
        {
            return new SymbolFilterData
            {
                Id = VisitorHelper.GetId(symbol),
                Kind = GetExtendedSymbolKindFromSymbol(symbol),
                Attributes = symbol.GetAttributes().Select(GetAttributeFilterData)
            };
        }

        public static AttributeFilterData GetAttributeFilterData(AttributeData attribute)
        {
            return new AttributeFilterData
            {
                Id = VisitorHelper.GetId(attribute.AttributeClass),
                ConstructorArguments = attribute.ConstructorArguments.Select(GetLiteralString),
                ConstructorNamedArguments = attribute.NamedArguments.ToDictionary(pair => pair.Key, pair => GetLiteralString(pair.Value))
            };
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

        private static string GetLiteralString(TypedConstant constant)
        {
            var type = constant.Type;
            var value = constant.Value;

            if (type.TypeKind == TypeKind.Enum)
            {
                var namedType = (INamedTypeSymbol)type;
                var pairs = (from member in namedType.GetMembers().OfType<IFieldSymbol>()
                             where member.IsConst && member.HasConstantValue
                             select Tuple.Create(member.Name, member.ConstantValue)).ToDictionary(tuple => tuple.Item2, tuple => tuple.Item1);

                return $"{VisitorHelper.GetId(namedType)}.{pairs[value]}";
            }

            if (value is ITypeSymbol)
            {
                return VisitorHelper.GetId((ITypeSymbol)value);
            }

            return value.ToString();
        }    
    }
}
