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

    /// <summary>
    /// Separates the assembly component of a UID from the namespace qualified name that follows it, as in
    /// <c>MyLib.Tools::MyLib.Tools.Widget</c>. It is deliberately not a dot: the component names an
    /// assembly, not a namespace, so it must not read as one.
    /// </summary>
    public const string AssemblyUidSeparator = "::";

    /// <summary>
    /// The assemblies whose APIs carry an assembly component in their UID, mapped to that component.
    /// A null value means the assembly's own name is used, which is the normal case.
    /// This is assigned once before metadata generation starts and is only read afterwards,
    /// so it is safe to read from the parallel API page generation.
    /// </summary>
    public static IReadOnlyDictionary<string, string> AssemblyUids { get; set; }

    /// <summary>
    /// The assembly component used for every API declared in <see cref="AssemblyUidOverrideAssemblies"/>.
    /// It takes precedence over <see cref="AssemblyUids"/>, which lets metadata items that document
    /// assemblies sharing an assembly name give them distinct UIDs.
    /// This is assigned once per metadata item, before its APIs are generated.
    /// </summary>
    public static string AssemblyUidOverride { get; set; }

    /// <summary>
    /// The assemblies <see cref="AssemblyUidOverride"/> applies to, i.e. the assemblies documented by the
    /// metadata item that is currently being processed.
    /// </summary>
    public static HashSet<IAssemblySymbol> AssemblyUidOverrideAssemblies { get; set; }

    private static bool IsAssemblyUidConfigured => AssemblyUids is { Count: > 0 } || !string.IsNullOrEmpty(AssemblyUidOverride);

    [GeneratedRegex(@"``\d+$")]
    private static partial Regex GenericMethodPostFix();

    public static string PathFriendlyId(string id)
    {
        // `:` is not a legal file name character, so the assembly component separator becomes dashes.
        // One dash per character, because `PathUtility.ToCleanUrlFileName` replaces each character it
        // rejects with a single dash, and it is what names the member pages that `memberLayout:
        // separatePages` splits out. The two have to agree or the hrefs miss those pages.
        return id.Replace(':', '-').Replace('`', '-').Replace('#', '-').Replace("*", "");
    }

    /// <summary>
    /// Removes the assembly component from a UID, leaving the namespace qualified name it addresses.
    /// A UID without an assembly component is returned unchanged.
    /// </summary>
    public static string TrimAssemblyUid(string uid)
    {
        if (uid is null)
        {
            return null;
        }

        var separator = uid.IndexOf(AssemblyUidSeparator, StringComparison.Ordinal);
        return separator < 0 ? uid : uid[(separator + AssemblyUidSeparator.Length)..];
    }

    public static string GetId(ISymbol symbol)
    {
        return GetId(symbol, applyAssemblyUid: true);
    }

    /// <summary>
    /// Gets the id of a symbol without applying any configured assembly component.
    /// API filters use this so that filter rules keep matching the actual API surface
    /// regardless of the configured assembly components.
    /// </summary>
    public static string GetRawId(ISymbol symbol)
    {
        return GetId(symbol, applyAssemblyUid: false);
    }

    private static string GetId(ISymbol symbol, bool applyAssemblyUid)
    {
        if (symbol == null)
        {
            return null;
        }

        if (symbol is INamespaceSymbol { IsGlobalNamespace: true })
        {
            // The global namespace of a qualified assembly is qualified too, otherwise two assemblies
            // whose APIs sit in the global namespace would share the one page named after
            // `globalNamespaceId`, which is the collision this is all meant to avoid.
            return applyAssemblyUid && !string.IsNullOrEmpty(GlobalNamespaceId) && GetAssemblyUid(symbol) is { } assemblyUid
                ? assemblyUid + AssemblyUidSeparator + GlobalNamespaceId
                : GlobalNamespaceId;
        }

        if (symbol is IAssemblySymbol assemblySymbol)
        {
            return assemblySymbol.MetadataName;
        }

        if (symbol is IDynamicTypeSymbol)
        {
            return "dynamic";
        }

        var id = GetDocumentationCommentId(symbol, applyAssemblyUid)?.Substring(2);

        if ((id is null) && (symbol is IFunctionPointerTypeSymbol functionPointerTypeSymbol))
        {
            // Roslyn doesn't currently support doc comments for function pointer type symbols
            // This returns just the stringified symbol to ensure the source and target parts
            // match for reference item merging.

            return functionPointerTypeSymbol.ToString();
        }

        return id;
    }

    private static string GetDocumentationCommentId(ISymbol symbol, bool applyAssemblyUid = true)
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

        if (applyAssemblyUid && GetAssemblyUid(symbol) is { } assemblyUid)
        {
            str = str.Insert(2, assemblyUid + AssemblyUidSeparator);
        }

        return str;
    }

    /// <summary>
    /// Gets the assembly component of the UID of <paramref name="symbol"/>, i.e. the configured component
    /// of the assembly that declares it, or <see langword="null"/> when it is not qualified.
    /// </summary>
    public static string GetAssemblyUid(ISymbol symbol)
    {
        if (symbol is null || !IsAssemblyUidConfigured)
        {
            return null;
        }

        // Type parameters are scoped to their declaring API, qualifying them would break the
        // `` `0 `` / ` ``0 ` references used by the documentation comment id format.
        if (symbol is ITypeParameterSymbol)
        {
            return null;
        }

        // ContainingAssembly is null for symbols that don't belong to a single assembly,
        // e.g. merged namespaces or some symbols reached through cref resolution.
        return symbol.ContainingAssembly is { } assembly ? GetAssemblyUid(assembly) : null;
    }

    /// <summary>
    /// Gets the assembly component configured for <paramref name="assembly"/>, or <see langword="null"/>
    /// when its APIs are not qualified by it.
    /// </summary>
    public static string GetAssemblyUid(IAssemblySymbol assembly)
    {
        if (assembly is null || !IsAssemblyUidConfigured)
        {
            return null;
        }

        // The component of the metadata item being processed wins, so that assemblies sharing an
        // assembly name can still be told apart by the item that documents each of them.
        if (!string.IsNullOrEmpty(AssemblyUidOverride) && AssemblyUidOverrideAssemblies is { } assemblies && assemblies.Contains(assembly))
        {
            return AssemblyUidOverride;
        }

        // A null value means the assembly is qualified by its own name, which is the normal case. The name
        // comes from the assembly rather than from the configured key, so its casing is always the real one.
        return AssemblyUids is { } assemblyUids && assemblyUids.TryGetValue(assembly.Name, out var component)
            ? component ?? assembly.Name
            : null;
    }

    /// <summary>
    /// Gets the assembly component of the UID of the API a documentation comment id (i.e. a cref) points
    /// to, or <see langword="null"/> when the target is not qualified or cannot be resolved.
    /// </summary>
    public static string GetAssemblyUidForCommentId(string commentId, Compilation compilation)
    {
        if (string.IsNullOrEmpty(commentId) || !IsAssemblyUidConfigured)
        {
            return null;
        }

        // `Overload:` is a docfx concept, Roslyn only knows about declaration id kinds.
        const string overloadPrefix = "Overload:";
        if (commentId.StartsWith(overloadPrefix, StringComparison.Ordinal))
        {
            var body = commentId[overloadPrefix.Length..];
            return GetAssemblyUid(ResolveDeclarationId($"M:{body}"))
                ?? GetAssemblyUid(ResolveDeclarationId($"P:{body}"));
        }

        return GetAssemblyUid(ResolveDeclarationId(commentId));

        ISymbol ResolveDeclarationId(string id) => DocumentationCommentId.GetFirstSymbolForDeclarationId(id, compilation);
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
            case TypeKind.Extension:
                return MemberType.Extension;
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
