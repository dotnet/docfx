// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public sealed class CompositeYamlModelGenerator : YamlModelGenerator
    {
        private readonly List<SimpleYamlModelGenerator> _generators;

        public CompositeYamlModelGenerator(IEnumerable<SimpleYamlModelGenerator> generators)
        {
            if (generators == null)
            {
                throw new ArgumentNullException("generators");
            }
            _generators = generators.Where(g => g != null).ToList();
            if (_generators.Count == 0)
            {
                throw new ArgumentException("generators");
            }
        }

        public CompositeYamlModelGenerator(params SimpleYamlModelGenerator[] generators)
            : this((IEnumerable<SimpleYamlModelGenerator>)generators)
        {
        }

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.DefaultVisit(symbol, item, adapter);
            }
        }

        public override void GenerateNamedType(INamedTypeSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateNamedType(symbol, item, adapter);
            }
        }

        public override void GenerateMethod(IMethodSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateMethod(symbol, item, adapter);
            }
        }

        public override void GenerateField(IFieldSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateField(symbol, item, adapter);
            }
        }

        public override void GenerateProperty(IPropertySymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateProperty(symbol, item, adapter);
            }
        }

        public override void GenerateEvent(IEventSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateEvent(symbol, item, adapter);
            }
        }

        internal override void GenerateReferenceInternal(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateReferenceInternal(symbol, reference, adapter, asOverload);
            }
        }

        internal override void GenerateSyntax(MemberType type, ISymbol symbol, SyntaxDetail syntax, SymbolVisitorAdapter adapter)
        {
            foreach (var generator in _generators)
            {
                generator.GenerateSyntax(type, symbol, syntax, adapter);
            }
        }

        public static CompositeYamlModelGenerator operator +(CompositeYamlModelGenerator left, SimpleYamlModelGenerator right)
        {
            return new CompositeYamlModelGenerator(left._generators.Concat(new[] { right }));
        }
    }
}
