// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Microsoft.DocAsCode.Dotnet
{
    internal class SymbolFilter
    {
        private readonly DotnetApiCatalogOptions _options;
        private readonly ConfigFilterRule? _filterRule;

        private readonly ConcurrentDictionary<ISymbol, bool> _cache = new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<ISymbol, bool> _attributeCache = new(SymbolEqualityComparer.Default);

        public SymbolFilter(ExtractMetadataOptions config, DotnetApiCatalogOptions options)
        {
            _options = options;
            _filterRule = config.DisableDefaultFilter ? null : ConfigFilterRule.LoadWithDefaults(config.FilterConfigFile);
        }

        public bool IncludeApi(ISymbol symbol)
        {
            return IsSymbolAccessible(symbol) && IncludeApiCore(symbol);

            bool IncludeApiCore(ISymbol symbol)
            {
                return _cache.GetOrAdd(symbol, _ => _options.IncludeApi?.Invoke(_) switch
                {
                    SymbolIncludeState.Include => true,
                    SymbolIncludeState.Exclude => false,
                    _ => IncludeApiDefault(symbol),
                });
            }

            bool IncludeApiDefault(ISymbol symbol)
            {
                if (_filterRule is not null && !_filterRule.CanVisitApi(RoslynFilterData.GetSymbolFilterData(symbol)))
                    return false;

                return symbol.ContainingSymbol is null || IncludeApiCore(symbol.ContainingSymbol);
            }
        }

        public bool IncludeAttribute(ISymbol symbol)
        {
            return IsSymbolAccessible(symbol) && IncludeAttributeCore(symbol);

            bool IncludeAttributeCore(ISymbol symbol)
            {
                return _attributeCache.GetOrAdd(symbol, _ => _options.IncludeAttribute?.Invoke(_) switch
                {
                    SymbolIncludeState.Include => true,
                    SymbolIncludeState.Exclude => false,
                    _ => IncludeAttributeDefault(symbol),
                });
            }

            bool IncludeAttributeDefault(ISymbol symbol)
            {
                if (_filterRule is not null && !_filterRule.CanVisitAttribute(RoslynFilterData.GetSymbolFilterData(symbol)))
                    return false;

                return symbol.ContainingSymbol is null || IncludeAttributeCore(symbol.ContainingSymbol);
            }
        }

        private static bool IsSymbolAccessible(ISymbol symbol)
        {
            if (symbol.IsImplicitlyDeclared && symbol.Kind is not SymbolKind.Namespace)
                return false;

            if (symbol.GetDisplayAccessibility() is null)
                return false;

            return symbol.ContainingSymbol is null || IsSymbolAccessible(symbol.ContainingSymbol);
        }
    }
}
