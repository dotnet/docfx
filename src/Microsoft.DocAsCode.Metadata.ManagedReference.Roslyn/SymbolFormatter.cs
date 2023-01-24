using System;
using System.Collections.Immutable;
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
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        private static readonly SymbolDisplayFormat s_nameWithTypeFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        private static readonly SymbolDisplayFormat s_qualifiedNameFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType);

        private static readonly SymbolDisplayFormat s_namespaceFormat = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static readonly SymbolDisplayFormat s_methodNameFormat = s_nameFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        private static readonly SymbolDisplayFormat s_methodNameWithTypeFormat = s_nameWithTypeFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        private static readonly SymbolDisplayFormat s_methodQualifiedNameFormat = s_qualifiedNameFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

        public static string GetName(ISymbol symbol, SyntaxLanguage language)
        {
            return GetNameParts(symbol, language).ToDisplayString();
        }

        public static ImmutableArray<SymbolDisplayPart> GetNameParts(ISymbol symbol, SyntaxLanguage language)
        {
            return GetDisplayParts(symbol, language, symbol.Kind switch
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

        public static ImmutableArray<SymbolDisplayPart> GetNameWithTypeParts(ISymbol symbol, SyntaxLanguage language)
        {
            return GetDisplayParts(symbol, language, symbol.Kind switch
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

        public static ImmutableArray<SymbolDisplayPart> GetQualifiedNameParts(ISymbol symbol, SyntaxLanguage language)
        {
            return GetDisplayParts(symbol, language, symbol.Kind switch
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

        private static ImmutableArray<SymbolDisplayPart> GetDisplayParts(ISymbol symbol, SyntaxLanguage language, SymbolDisplayFormat format)
        {
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
