using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    internal static class SymbolFormatter
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
            var format = symbol.Kind switch
            {
                SymbolKind.NamedType => s_nameWithTypeFormat,
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodNameFormat,
                _ => s_nameFormat,
            };

            try
            {
                return language switch
                {
                    SyntaxLanguage.CSharp => CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(symbol, format),
                    SyntaxLanguage.VB => CodeAnalysis.VisualBasic.SymbolDisplay.ToDisplayString(symbol, format),
                    _ => throw new NotSupportedException(),
                };
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }

        public static string GetNameWithType(ISymbol symbol, SyntaxLanguage language)
        {
            var format = symbol.Kind switch
            {
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodNameWithTypeFormat,
                _ => s_nameWithTypeFormat,
            };

            try
            {
                return language switch
                {
                    SyntaxLanguage.CSharp => CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(symbol, format),
                    SyntaxLanguage.VB => CodeAnalysis.VisualBasic.SymbolDisplay.ToDisplayString(symbol, format),
                    _ => throw new NotSupportedException(),
                };
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }

        public static string GetQualifiedName(ISymbol symbol, SyntaxLanguage language)
        {
            var format = symbol.Kind switch
            {
                SymbolKind.Namespace => s_namespaceFormat,
                SymbolKind.Method => s_methodQualifiedNameFormat,
                _ => s_qualifiedNameFormat,
            };

            try
            {
                return language switch
                {
                    SyntaxLanguage.CSharp => CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(symbol, format),
                    SyntaxLanguage.VB => CodeAnalysis.VisualBasic.SymbolDisplay.ToDisplayString(symbol, format),
                    _ => throw new NotSupportedException(),
                };
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }
    }
}
