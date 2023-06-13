// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

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

    public void GenerateReference(ISymbol symbol, ReferenceItem reference, bool asOverload)
    {
        if (!reference.NameParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.NameParts.Add(SyntaxLanguage.CSharp, new());
        if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.NameWithTypeParts.Add(SyntaxLanguage.CSharp, new());
        if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.CSharp))
            reference.QualifiedNameParts.Add(SyntaxLanguage.CSharp, new());

        reference.NameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);
        reference.NameWithTypeParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);
        reference.QualifiedNameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);

        if (!reference.NameParts.ContainsKey(SyntaxLanguage.VB))
            reference.NameParts.Add(SyntaxLanguage.VB, new());
        if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.VB))
            reference.NameWithTypeParts.Add(SyntaxLanguage.VB, new());
        if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.VB))
            reference.QualifiedNameParts.Add(SyntaxLanguage.VB, new());

        reference.NameParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);
        reference.NameWithTypeParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);
        reference.QualifiedNameParts[SyntaxLanguage.VB] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(_compilation, _memberLayout, _allAssemblies, asOverload);
    }

    public void GenerateSyntax(ISymbol symbol, SyntaxDetail syntax, SymbolFilter filter)
    {
        syntax.Content[SyntaxLanguage.CSharp] = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, filter);
        syntax.Content[SyntaxLanguage.VB] = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.VB, filter);
    }

    public string AddReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolVisitorAdapter adapter)
    {
        var id = VisitorHelper.GetId(symbol);
        var reference = new ReferenceItem
        {
            NameParts = new(),
            NameWithTypeParts = new(),
            QualifiedNameParts = new(),
            IsDefinition = symbol.IsDefinition,
            CommentId = VisitorHelper.GetCommentId(symbol)
        };
        GenerateReference(symbol, reference, false);

        if (!references.ContainsKey(id))
        {
            references[id] = reference;
        }
        else
        {
            references[id].Merge(reference);
        }

        return id;
    }

    public string AddOverloadReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolVisitorAdapter adapter)
    {
        var uidBody = VisitorHelper.GetOverloadIdBody(symbol);
        var reference = new ReferenceItem
        {
            NameParts = new(),
            NameWithTypeParts = new(),
            QualifiedNameParts = new(),
            IsDefinition = true,
            CommentId = "Overload:" + uidBody
        };

        GenerateReference(symbol, reference, true);

        var uid = uidBody + "*";
        if (!references.ContainsKey(uid))
        {
            references[uid] = reference;
        }
        else
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
        SymbolVisitorAdapter adapter)
    {
        var rawId = VisitorHelper.GetId(symbol);
        var id = SpecIdHelper.GetSpecId(symbol, typeGenericParameters, methodGenericParameters);
        if (string.IsNullOrEmpty(id))
        {
            throw new InvalidDataException($"Fail to parse id for symbol {symbol.MetadataName} in namespace {symbol.ContainingSymbol?.MetadataName}.");
        }
        var reference = new ReferenceItem
        {
            NameParts = new(),
            NameWithTypeParts = new(),
            QualifiedNameParts = new(),
        };
        GenerateReference(symbol, reference, false);
        var originalSymbol = symbol;
        var reducedFrom = (symbol as IMethodSymbol)?.ReducedFrom;
        if (reducedFrom != null)
        {
            originalSymbol = reducedFrom;
        }
        reference.IsDefinition = originalSymbol.Equals(symbol, SymbolEqualityComparer.Default) && (id == rawId) && (symbol.IsDefinition || VisitorHelper.GetId(symbol.OriginalDefinition) == rawId);

        if (!reference.IsDefinition.Value && rawId != null)
        {
            reference.Definition = AddReference(originalSymbol.OriginalDefinition, references, adapter);
        }

        reference.Parent = GetReferenceParent(originalSymbol, typeGenericParameters, methodGenericParameters, references, adapter);
        reference.CommentId = VisitorHelper.GetCommentId(originalSymbol);

        if (!references.ContainsKey(id))
        {
            references[id] = reference;
        }
        else
        {
            references[id].Merge(reference);
        }

        return id;
    }

    private string GetReferenceParent(ISymbol symbol,
        IReadOnlyList<string> typeGenericParameters,
        IReadOnlyList<string> methodGenericParameters,
        Dictionary<string, ReferenceItem> references,
        SymbolVisitorAdapter adapter)
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
                    return AddSpecReference(parentSymbol, typeGenericParameters, methodGenericParameters, references, adapter); ;
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
