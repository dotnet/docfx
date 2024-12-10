// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.Common.Git;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet;

internal static partial class VisitorHelper
{
    public static string GlobalNamespaceId { get; set; }

    [GeneratedRegex(@"``\d+$")]
    private static partial Regex GenericMethodPostFix();

    public static string PathFriendlyId(string id)
    {
        return id.Replace('`', '-').Replace('#', '-').Replace("*", "");
    }

    public static string GetId(ISymbol symbol)
    {
        if (symbol == null)
        {
            return null;
        }

        if (symbol is INamespaceSymbol { IsGlobalNamespace: true })
        {
            return GlobalNamespaceId;
        }

        if (symbol is IAssemblySymbol assemblySymbol)
        {
            return assemblySymbol.MetadataName;
        }

        if (symbol is IDynamicTypeSymbol)
        {
            return "dynamic";
        }

        var id = GetDocumentationCommentId(symbol)?.Substring(2);

        if ((id is null) && (symbol is IFunctionPointerTypeSymbol functionPointerTypeSymbol))
        {
            // Roslyn doesn't currently support doc comments for function pointer type symbols
            // This returns just the stringified symbol to ensure the source and target parts
            // match for reference item merging.

            return functionPointerTypeSymbol.ToString();
        }

        return id;
    }

    private static string GetDocumentationCommentId(ISymbol symbol)
    {
        string str = symbol.GetDocumentationCommentId();
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        if (InGlobalNamespace(symbol) && !string.IsNullOrEmpty(GlobalNamespaceId))
        {
            bool isNamespace = symbol is INamespaceSymbol;
            bool isTypeParameter = symbol is ITypeParameterSymbol;
            if (!isNamespace && !isTypeParameter)
            {
                str = str.Insert(2, GlobalNamespaceId + ".");
            }
        }
        return str;
    }

    public static string GetCommentId(ISymbol symbol)
    {
        if (symbol == null || symbol is IAssemblySymbol)
        {
            return null;
        }

        if (symbol is IDynamicTypeSymbol)
        {
            return "T:" + typeof(object).FullName;
        }

        return GetDocumentationCommentId(symbol);
    }

    public static string GetOverloadId(ISymbol symbol)
    {
        return GetOverloadIdBody(symbol) + "*";
    }

    public static string GetOverloadIdBody(ISymbol symbol)
    {
        var id = GetId(symbol);
        var uidBody = id;
        {
            var index = uidBody.IndexOf('(');
            if (index != -1)
            {
                uidBody = uidBody.Remove(index);
            }
        }
        uidBody = GenericMethodPostFix().Replace(uidBody, string.Empty);
        return uidBody;
    }

    public static ApiParameter GetParameterDescription(ISymbol symbol, MetadataItem item, string id, bool isReturn)
    {
        string comment = isReturn ? item.CommentModel?.Returns : item.CommentModel?.GetParameter(symbol.Name);
        return new ApiParameter
        {
            Name = isReturn ? null : symbol.Name,
            Type = id,
            Description = comment,
        };
    }

    public static ApiParameter GetTypeParameterDescription(ITypeParameterSymbol symbol, MetadataItem item)
    {
        string comment = item.CommentModel?.GetTypeParameter(symbol.Name);
        return new ApiParameter
        {
            Name = symbol.Name,
            Description = comment,
        };
    }

    public static SourceDetail GetSourceDetail(ISymbol symbol, Compilation compilation)
    {
        // For namespace, definition is meaningless
        if (symbol == null || symbol.Kind == SymbolKind.Namespace)
        {
            return null;
        }

        var syntaxRef = symbol.DeclaringSyntaxReferences.LastOrDefault();
        if (symbol.IsExtern || syntaxRef == null)
        {
            if (SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, symbol) is string url)
            {
                return new() { Href = url };
            }

            return null;
        }

        var syntaxNode = syntaxRef.GetSyntax();
        Debug.Assert(syntaxNode != null);
        if (syntaxNode != null)
        {
            var source = new SourceDetail
            {
                StartLine = syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line,
                Path = syntaxNode.SyntaxTree.FilePath,
                Name = symbol.Name
            };

            source.Remote = GitUtility.TryGetFileDetail(source.Path);
            if (source.Remote != null)
            {
                source.Path = PathUtility.FormatPath(source.Path, UriKind.Relative, EnvironmentContext.BaseDirectory);
            }
            return source;
        }

        return null;
    }

    public static MemberType GetMemberTypeFromTypeKind(TypeKind typeKind)
    {
        switch (typeKind)
        {
            case TypeKind.Module:
            case TypeKind.Class:
                return MemberType.Class;
            case TypeKind.Enum:
                return MemberType.Enum;
            case TypeKind.Interface:
                return MemberType.Interface;
            case TypeKind.Struct:
                return MemberType.Struct;
            case TypeKind.Delegate:
                return MemberType.Delegate;
            default:
                return MemberType.Default;
        }
    }

    public static bool InGlobalNamespace(ISymbol symbol)
    {
        Debug.Assert(symbol != null);

        return symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace;
    }
}
