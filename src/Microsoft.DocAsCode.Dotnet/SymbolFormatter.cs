﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DocAsCode.Dotnet;

internal static partial class SymbolFormatter
{
    private static readonly SymbolDisplayFormat s_nameFormat = new(
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod);

    private static readonly SymbolDisplayFormat s_nameWithTypeFormat = s_nameFormat
        .AddMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)
        .WithTypeQualificationStyle(SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    private static readonly SymbolDisplayFormat s_qualifiedNameFormat = s_nameWithTypeFormat
        .WithTypeQualificationStyle(SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

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

    private static readonly SymbolDisplayFormat s_linkItemQualifiedNameFormat = s_linkItemNameWithTypeFormat
        .WithTypeQualificationStyle(SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static string GetName(ISymbol symbol, SyntaxLanguage language)
    {
        return GetNameParts(symbol, language).ToDisplayString();
    }

    public static ImmutableArray<SymbolDisplayPart> GetNameParts(
        ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true, bool overload = false)
    {
        return GetDisplayParts(symbol, language, nullableReferenceType, overload, symbol.Kind switch
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

    public static ImmutableArray<SymbolDisplayPart> GetNameWithTypeParts(
        ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true, bool overload = false)
    {
        return GetDisplayParts(symbol, language, nullableReferenceType, overload, symbol.Kind switch
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

    public static ImmutableArray<SymbolDisplayPart> GetQualifiedNameParts(
        ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType = true, bool overload = false)
    {
        return GetDisplayParts(symbol, language, nullableReferenceType, overload, symbol.Kind switch
        {
            SymbolKind.Namespace => s_namespaceFormat,
            SymbolKind.Method => s_methodQualifiedNameFormat,
            _ => s_qualifiedNameFormat,
        });
    }

    public static string GetSyntax(ISymbol symbol, SyntaxLanguage language, SymbolFilter filter)
    {
        return GetSyntaxParts(symbol, language, filter).ToDisplayString();
    }

    public static ImmutableArray<SymbolDisplayPart> GetSyntaxParts(ISymbol symbol, SyntaxLanguage language, SymbolFilter filter)
    {
        try
        {
            return new SyntaxFormatter { Language = language, Filter = filter }.GetSyntax(symbol);
        }
        catch (InvalidOperationException)
        {
            return ImmutableArray<SymbolDisplayPart>.Empty;
        }
    }

    public static List<LinkItem> ToLinkItems(this ImmutableArray<SymbolDisplayPart> parts,
        Compilation compilation, MemberLayout memberLayout, HashSet<IAssemblySymbol> allAssemblies, bool overload)
    {
        var result = new List<LinkItem>();
        foreach (var part in parts)
        {
            result.Add(ToLinkItem(part));

            if (overload && part.Kind is SymbolDisplayPartKind.MethodName)
                break;
        }
        return result;

        LinkItem ToLinkItem(SymbolDisplayPart part)
        {
            var symbol = part.Symbol;
            if (symbol is null || part.Kind is SymbolDisplayPartKind.TypeParameterName)
                return new() { DisplayName = part.ToString() };

            if (symbol is INamedTypeSymbol type && type.IsGenericType)
                symbol = type.ConstructedFrom;

            return new()
            {
                Name = overload ? VisitorHelper.GetOverloadId(symbol) : VisitorHelper.GetId(symbol),
                DisplayName = part.ToString(),
                Href = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, memberLayout, allAssemblies),
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            };
        }
    }

    private static ImmutableArray<SymbolDisplayPart> GetDisplayParts(
        ISymbol symbol, SyntaxLanguage language, bool nullableReferenceType, bool overload, SymbolDisplayFormat format)
    {
        if (!nullableReferenceType)
            format = format.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        if (overload)
            format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeParameters);

        try
        {
            var result = language switch
            {
                SyntaxLanguage.VB => VB.SymbolDisplay.ToDisplayParts(symbol, format),
                _ => CS.SymbolDisplay.ToDisplayParts(symbol, format),
            };

            if (overload && language is SyntaxLanguage.CSharp && symbol.IsCastOperator())
                return GetCastOperatorOverloadDisplayParts(result);

            return result;
        }
        catch (InvalidOperationException)
        {
            return ImmutableArray<SymbolDisplayPart>.Empty;
        }

        static ImmutableArray<SymbolDisplayPart> GetCastOperatorOverloadDisplayParts(ImmutableArray<SymbolDisplayPart> parts)
        {
            // Convert from "explicit operator Bar" to "explicit operator", for lack of disabling return type in SymbolDisplay.
            var endIndex = parts.Length;
            while (--endIndex >= 0)
            {
                var part = parts[endIndex];
                if (part.Kind is SymbolDisplayPartKind.Keyword && part.ToString() is "operator" or "checked")
                    break;
            }
            return parts.Take(endIndex + 1).ToImmutableArray();
        }
    }

    private static SymbolDisplayFormat WithTypeQualificationStyle(this SymbolDisplayFormat format, SymbolDisplayTypeQualificationStyle style)
    {
        return new(
            format.GlobalNamespaceStyle,
            style,
            format.GenericsOptions,
            format.MemberOptions,
            format.DelegateStyle,
            format.ExtensionMethodStyle,
            format.ParameterOptions,
            format.PropertyStyle,
            format.LocalOptions,
            format.KindOptions,
            format.MiscellaneousOptions);
    }
}
