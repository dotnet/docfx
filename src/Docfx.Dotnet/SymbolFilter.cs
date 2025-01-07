// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

internal class SymbolFilter
{
    private readonly ExtractMetadataConfig _config;
    private readonly DotnetApiOptions _options;
    private readonly ConfigFilterRule? _filterRule;

    private readonly ConcurrentDictionary<ISymbol, bool> _cache = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ISymbol, bool> _attributeCache = new(SymbolEqualityComparer.Default);

    public SymbolFilter(ExtractMetadataConfig config, DotnetApiOptions options)
    {
        _options = options;
        _config = config;
        _filterRule = config.DisableDefaultFilter
                            ? ConfigFilterRule.Load(config.FilterConfigFile)
                            : ConfigFilterRule.LoadWithDefaults(config.FilterConfigFile);
    }

    public bool IncludeApi(ISymbol symbol)
    {
        return _cache.GetOrAdd(symbol, _ =>
        {
            return !IsCompilerGeneratedDisplayClass(symbol) &&
                IsSymbolAccessible(symbol) &&
                !HasExcludeDocumentComment(symbol) &&
                _options.IncludeApi?.Invoke(_) switch
                {
                    SymbolIncludeState.Include => true,
                    SymbolIncludeState.Exclude => false,
                    _ => IncludeApiDefault(symbol),
                };
        });

        bool IncludeApiDefault(ISymbol symbol)
        {
            if (_filterRule is not null && !_filterRule.CanVisitApi(RoslynFilterData.GetSymbolFilterData(symbol)))
                return false;

            return symbol.ContainingSymbol is null || IncludeApi(symbol.ContainingSymbol);
        }

        static bool IsCompilerGeneratedDisplayClass(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.NamedType && (symbol.Name.Contains('<') || symbol.Name.Contains('>'));
        }
    }

    public bool IncludeAttribute(ISymbol symbol)
    {
        return _attributeCache.GetOrAdd(symbol, _ =>
        {
            return IsSymbolAccessible(symbol) && !HasExcludeDocumentComment(symbol) && _options.IncludeAttribute?.Invoke(_) switch
            {
                SymbolIncludeState.Include => true,
                SymbolIncludeState.Exclude => false,
                _ => IncludeAttributeDefault(symbol),
            };
        });

        bool IncludeAttributeDefault(ISymbol symbol)
        {
            if (_filterRule is not null && !_filterRule.CanVisitAttribute(RoslynFilterData.GetSymbolFilterData(symbol)))
                return false;

            return symbol.ContainingSymbol is null || IncludeAttribute(symbol.ContainingSymbol);
        }
    }

    public Accessibility? GetDisplayAccessibility(ISymbol symbol)
    {
        if (_config.IncludePrivateMembers)
            return symbol.DeclaredAccessibility;

        // Hide internal or private APIs
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.NotApplicable => Accessibility.NotApplicable,
            Accessibility.Public => Accessibility.Public,
            Accessibility.Protected => Accessibility.Protected,
            Accessibility.ProtectedOrInternal => Accessibility.Protected,
            _ => null,
        };
    }

    private bool IsSymbolAccessible(ISymbol symbol)
    {
        // TODO: should we include implicitly declared members like constructors? They are part of the API contract.
        if (symbol is { IsImplicitlyDeclared: true, Kind: not SymbolKind.Namespace })
            return false;

        if (_config.IncludeExplicitInterfaceImplementations &&
            SymbolHelper.TryGetExplicitInterfaceImplementations(symbol, out var eiis) &&
            IsEiiAndIncludesContainingSymbols(eiis))
        {
            return true;
        }

        if (_config.IncludePrivateMembers)
        {
            if (SymbolHelper.TryGetExplicitInterfaceImplementations(symbol, out eiis))
                return IncludesContainingSymbols(eiis);

            return true;
        }

        if (GetDisplayAccessibility(symbol) is null)
            return false;

        return symbol.ContainingSymbol is null || IsSymbolAccessible(symbol.ContainingSymbol);

        bool IncludesContainingSymbols(IEnumerable<ISymbol> symbols)
        {
            return !symbols.Any() || symbols.All(s => IncludeApi(s.ContainingSymbol));
        }

        bool IsEiiAndIncludesContainingSymbols(IEnumerable<ISymbol> symbols)
        {
            return symbols.Any() && symbols.All(s => IncludeApi(s.ContainingSymbol));
        }
    }

    private static bool HasExcludeDocumentComment(ISymbol symbol)
    {
        return symbol.GetDocumentationCommentXml() is { } xml && (
            xml.Contains("<exclude/>") ||
            xml.Contains("<exclude>") ||
            xml.Contains("<exclude "));
    }
}
