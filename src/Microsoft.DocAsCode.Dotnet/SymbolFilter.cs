// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            return _options.ShowApi?.Invoke(symbol) switch
            {
                SymbolShowState.Show => true,
                SymbolShowState.Hide => false,
                _ => symbol.IncludeSymbol() && IncludeApiCore(symbol),
            };
        }

        public bool IncludeAttribute(IMethodSymbol symbol)
        {
            return _options.ShowAttribute?.Invoke(symbol) switch
            {
                SymbolShowState.Show => true,
                SymbolShowState.Hide => false,
                _ => symbol.IncludeSymbol() && IncludeAttributeCore(symbol),
            };
        }

        private bool IncludeApiCore(ISymbol symbol)
        {
            if (_filterRule is null)
                return false;

            return _cache.GetOrAdd(symbol, _ => _filterRule.CanVisitApi(RoslynFilterData.GetSymbolFilterData(_)));
        }

        private bool IncludeAttributeCore(ISymbol symbol)
        {
            if (_filterRule is null)
                return false;

            return _attributeCache.GetOrAdd(symbol, _ => _filterRule.CanVisitAttribute(RoslynFilterData.GetSymbolFilterData(_)));
        }
    }
}
