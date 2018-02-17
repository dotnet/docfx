// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.CodeAnalysis;

    public class ConfigFilterVisitor : DelegatingFilterVisitor
    {
        private string _configFile;
        private ConfigFilterRule _configRule;

        public ConfigFilterVisitor(IFilterVisitor inner, string configFile)
            : base(inner)
        {
            _configFile = configFile;
            _configRule = ConfigFilterRule.Load(configFile);
        }

        public ConfigFilterVisitor(IFilterVisitor inner, ConfigFilterRule rule)
            : base(inner)
        {
            _configRule = rule;
        }

        protected override bool CanVisitApiCore(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if (!Inner.CanVisitApi(symbol, wantProtectedMember, outer))
            {
                return false;
            }

            var symbolFilterData = RoslynFilterData.GetSymbolFilterData(symbol);
            return _configRule.CanVisitApi(symbolFilterData);
        }

        protected override bool CanVisitAttributeCore(ISymbol symbol, bool wantProtectedMember, IFilterVisitor outer)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if (!Inner.CanVisitAttribute(symbol, wantProtectedMember, outer))
            {
                return false;
            }

            var symbolFilterData = RoslynFilterData.GetSymbolFilterData(symbol);
            return _configRule.CanVisitAttribute(symbolFilterData);
        }

    }
}