using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    internal static partial class SymbolFormatter
    {
        private static readonly SymbolDisplayFormat s_nameFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod);

        private static readonly SymbolDisplayFormat s_nameWithTypeFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod);

        private static readonly SymbolDisplayFormat s_qualifiedNameFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod);

        private static readonly SymbolDisplayFormat s_namespaceFormat = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static readonly SymbolDisplayFormat s_methodNameFormat = s_nameFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        private static readonly SymbolDisplayFormat s_methodNameWithTypeFormat = s_nameWithTypeFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        private static readonly SymbolDisplayFormat s_methodQualifiedNameFormat = s_qualifiedNameFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        private static readonly SymbolDisplayFormat s_linkItemNameWithTypeFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        private static readonly SymbolDisplayFormat s_linkItemQualifiedNameFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public static string GetName(ISymbol symbol, SyntaxLanguage language)
        {
            return GetNameParts(symbol, language).ToDisplayString();
        }

        public static ImmutableArray<SymbolDisplayPart> GetNameParts(ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true)
        {
            return GetDisplayParts(symbol, language, nullableReferenceType, symbol.Kind switch
            {
                SymbolKind.NamedType => s_nameWithTypeFormat,
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodNameFormat,
                _ => s_nameFormat,
            });
        }

        public static string GetNameWithType(ISymbol symbol, SyntaxLanguage language)
        {
            return GetNameWithTypeParts(symbol, language).ToDisplayString();
        }

        public static ImmutableArray<SymbolDisplayPart> GetNameWithTypeParts(ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true)
        {
            return GetDisplayParts(symbol, language, nullableReferenceType, symbol.Kind switch
            {
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodNameWithTypeFormat,
                _ => s_nameWithTypeFormat,
            });
        }

        public static string GetQualifiedName(ISymbol symbol, SyntaxLanguage language)
        {
            return GetQualifiedNameParts(symbol, language).ToDisplayString();
        }

        public static ImmutableArray<SymbolDisplayPart> GetQualifiedNameParts(ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true)
        {
            return GetDisplayParts(symbol, language, nullableReferenceType, symbol.Kind switch
            {
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodQualifiedNameFormat,
                _ => s_qualifiedNameFormat,
            });
        }

        public static string GetSyntax(ISymbol symbol, SyntaxLanguage language, IFilterVisitor apiFilter)
        {
            return GetSyntaxParts(symbol, language, apiFilter).ToDisplayString();
        }

        public static ImmutableArray<SymbolDisplayPart> GetSyntaxParts(ISymbol symbol, SyntaxLanguage language, IFilterVisitor apiFilter)
        {
            try
            {
                return new SyntaxFormatter { Language = language, ApiFilter = apiFilter }.GetSyntax(symbol);
            }
            catch (InvalidOperationException)
            {
                return ImmutableArray<SymbolDisplayPart>.Empty;
            }
        }

        public static List<LinkItem> ToLinkItems(this ImmutableArray<SymbolDisplayPart> parts, SyntaxLanguage language, bool overload)
        {
            var result = new List<LinkItem>();
            foreach (var part in parts)
            {
                // Strip type parameters, parameters for overloads
                if (overload && IsStartOfCSharpConstructor(part))
                    break;

                result.Add(ToLinkItem(part));

                if (overload && part.Kind is SymbolDisplayPartKind.MethodName)
                    break;

                if (overload && IsVisualBasicConstructor(part))
                    break;
            }
            return result;

            bool IsStartOfCSharpConstructor(SymbolDisplayPart part)
            {
                return language is SyntaxLanguage.CSharp && part.Kind is SymbolDisplayPartKind.Punctuation && part.ToString() == "(";
            }

            bool IsVisualBasicConstructor(SymbolDisplayPart part)
            {
                return language is SyntaxLanguage.VB && part.Kind is SymbolDisplayPartKind.Keyword && part.ToString() == "New";
            }

            LinkItem ToLinkItem(SymbolDisplayPart part)
            {
                var symbol = part.Symbol;
                if (symbol is null || part.Kind is SymbolDisplayPartKind.TypeParameterName)
                {
                    return new() { DisplayName = part.ToString() };
                }

                if (symbol is INamedTypeSymbol type && type.IsGenericType)
                {
                    symbol = type.ConstructedFrom;
                }

                return new()
                {
                    Name = overload ? VisitorHelper.GetOverloadId(symbol) : VisitorHelper.GetId(symbol),
                    DisplayName = part.ToString(),
                    IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
                };
            }
        }

        private static ImmutableArray<SymbolDisplayPart> GetDisplayParts(ISymbol symbol, SyntaxLanguage language, SymbolDisplayFormat format)
        {
            return GetDisplayParts(symbol, language, true, format);
        }

        private static ImmutableArray<SymbolDisplayPart> GetDisplayParts(ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType, SymbolDisplayFormat format)
        {
            if (!nullableReferenceType)
            {
                format = format.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            }

            try
            {
                return language switch
                {
                    SyntaxLanguage.VB => VB.SymbolDisplay.ToDisplayParts(symbol, format),
                    _ => CS.SymbolDisplay.ToDisplayParts(symbol, format),
                };
            }
            catch (InvalidOperationException)
            {
                return ImmutableArray<SymbolDisplayPart>.Empty;
            }
        }
    }
}
