// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet;

internal class YamlModelGenerator
{
    private readonly Compilation _compilation;
    private readonly MemberLayout _memberLayout;
    private readonly HashSet<IAssemblySymbol> _allAssemblies;

    public YamlModelGenerator(Compilation compilation, MemberLayout memberLayout, HashSet<IAssemblySymbol> allAssemblies)
    {
        _compilation = compilation;
        _memberLayout = memberLayout;
        _allAssemblies = allAssemblies;
    }

    public void DefaultVisit(ISymbol symbol, MetadataItem item)
    {
        item.DisplayNames[SyntaxLanguage.CSharp] = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp);
        item.DisplayNamesWithType[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
        item.DisplayQualifiedNames[SyntaxLanguage.CSharp] = SymbolFormatter.GetQualifiedName(symbol, SyntaxLanguage.CSharp);

        item.DisplayNames[SyntaxLanguage.VB] = SymbolFormatter.GetName(symbol, SyntaxLanguage.VB);
        item.DisplayNamesWithType[SyntaxLanguage.VB] = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.VB);
        item.DisplayQualifiedNames[SyntaxLanguage.VB] = SymbolFormatter.GetQualifiedName(symbol, SyntaxLanguage.VB);
    }

    public void GenerateReference(ISymbol symbol, ReferenceItem reference, bool asOverload, SymbolFilter filter)
    {
        if (!reference.NameParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.NameParts.Add(SyntaxLanguage.CSharp, []);
        if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.NameWithTypeParts.Add(SyntaxLanguage.CSharp, []);
        if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.QualifiedNameParts.Add(SyntaxLanguage.CSharp, []);

        reference.NameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);
        reference.NameWithTypeParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);
        reference.QualifiedNameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);

        if (!reference.NameParts.ContainsKey(SyntaxLanguage.VB))
            reference.NameParts.Add(SyntaxLanguage.VB, []);
        if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.VB))
            reference.NameWithTypeParts.Add(SyntaxLanguage.VB, []);
        if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.VB))
            reference.QualifiedNameParts.Add(SyntaxLanguage.VB, []);

        reference.NameParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);
        reference.NameWithTypeParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);
        reference.QualifiedNameParts[SyntaxLanguage.VB] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload, filter);
    }

    public void GenerateSyntax(ISymbol symbol, SyntaxDetail syntax, SymbolFilter filter)
    {
        syntax.Content[SyntaxLanguage.CSharp] = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, filter);
        syntax.Content[SyntaxLanguage.VB] = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.VB, filter);
    }

    public string AddReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolFilter filter)
    {
        var id = VisitorHelper.GetId(symbol);
        var reference = new ReferenceItem
        {
            NameParts = [],
            NameWithTypeParts = [],
            QualifiedNameParts = [],
            IsDefinition = symbol.IsDefinition,
            CommentId = VisitorHelper.GetCommentId(symbol)
        };
        GenerateReference(symbol, reference, false, filter);

        if (!references.TryAdd(id, reference))
        {
            references[id].Merge(reference);
        }

        return id;
    }

    public string AddOverloadReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolFilter filter)
    {
        var uidBody = VisitorHelper.GetOverloadIdBody(symbol);
        var reference = new ReferenceItem
        {
            NameParts = [],
            NameWithTypeParts = [],
            QualifiedNameParts = [],
            IsDefinition = true,
            CommentId = "Overload:" + uidBody
        };

        GenerateReference(symbol, reference, true, filter);

        var uid = uidBody + "*";
        if (!references.TryAdd(uid, reference))
        {
            references[uid].Merge(reference);
        }

        return uid;
    }

    public string AddSpecReference(
        ISymbol symbol,
        IReadOnlyList<string> typeGenericParameters,
        IReadOnlyList<string> methodGenericParameters,
        Dictionary<string, ReferenceItem> references,
        SymbolFilter filter)
    {
        var rawId = VisitorHelper.GetId(symbol);
        var id = SpecIdHelper.GetSpecId(symbol, typeGenericParameters, methodGenericParameters);
        if (string.IsNullOrEmpty(id))
        {
            throw new InvalidDataException($"Fail to parse id for symbol {symbol.MetadataName} in namespace {symbol.ContainingSymbol?.MetadataName}.");
        }
        var reference = new ReferenceItem
        {
            NameParts = [],
            NameWithTypeParts = [],
            QualifiedNameParts = [],
        };
        GenerateReference(symbol, reference, false, filter);
        var originalSymbol = symbol;
        var reducedFrom = (symbol as IMethodSymbol)?.ReducedFrom;
        if (reducedFrom != null)
        {
            originalSymbol = reducedFrom;
        }
        reference.IsDefinition = originalSymbol.Equals(symbol, SymbolEqualityComparer.Default) && (id == rawId) && (symbol.IsDefinition || VisitorHelper.GetId(symbol.OriginalDefinition) == rawId);

        if (!reference.IsDefinition.Value && rawId != null)
        {
            reference.Definition = AddReference(originalSymbol.OriginalDefinition, references, filter);
        }

        reference.Parent = GetReferenceParent(originalSymbol, typeGenericParameters, methodGenericParameters, references, filter);
        reference.CommentId = VisitorHelper.GetCommentId(originalSymbol);

        if (!references.TryAdd(id, reference))
        {
            references[id].Merge(reference);
        }

        return id;
    }

    private string GetReferenceParent(ISymbol symbol,
        IReadOnlyList<string> typeGenericParameters,
        IReadOnlyList<string> methodGenericParameters,
        Dictionary<string, ReferenceItem> references,
        SymbolFilter filter)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Event:
            case SymbolKind.Field:
            case SymbolKind.Method:
            case SymbolKind.NamedType:
            case SymbolKind.Property:
                {
                    var parentSymbol = symbol;
                    do
                    {
                        parentSymbol = parentSymbol.ContainingSymbol;
                    } while (parentSymbol.Kind == symbol.Kind); // the parent of nested type is namespace.
                    if (IsGlobalNamespace(parentSymbol))
                    {
                        return null;
                    }
                    return AddSpecReference(parentSymbol, typeGenericParameters, methodGenericParameters, references, filter);
                }
            default:
                return null;
        }
    }

    private static bool IsGlobalNamespace(ISymbol symbol)
    {
        return symbol is INamespaceSymbol { IsGlobalNamespace: true };
    }
}
