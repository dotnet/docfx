// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;

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
                ConstructorArguments = attribute.ConstructorArguments.Select(GetLiteralString).ToList(),
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
            if (constant.Kind == TypedConstantKind.Enum)
            {
                var namedType = (INamedTypeSymbol)constant.Type;
                var name = (from member in namedType.GetMembers().OfType<IFieldSymbol>()
                            where member.IsConst && member.HasConstantValue
                            where constant.Value.Equals(member.ConstantValue)
                            select member.Name).FirstOrDefault();

                if (name != null)
                {
                    return $"{VisitorHelper.GetId(namedType)}.{name}";
                }

                // todo : define filter data format (language neutral), just use number for combine case by now.
                // e.g.: [Flags] public enum E { X=1,Y=2,Z=4,YZ=6 }
                // Case: [E(E.X | E.Y)]
                // Case: [E((E)99)]
                // Case: [E(E.X | E.YZ)]
                return constant.Value.ToString();
            }

            if (constant.Kind == TypedConstantKind.Array)
            {
                if (constant.Values.IsDefaultOrEmpty)
                {
                    return "";
                }

                return string.Join(",", constant.Values.Select(GetLiteralString));
            }

            var value = constant.Value;
            if (value is ISymbol)
            {
                return VisitorHelper.GetId(constant.Value as ISymbol);
            }

            return value?.ToString() ?? "null";
        }
    }
}
