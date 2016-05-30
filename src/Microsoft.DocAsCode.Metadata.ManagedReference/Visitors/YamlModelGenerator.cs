// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public abstract class YamlModelGenerator
    {
        internal YamlModelGenerator()
        {
        }

        public virtual void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        public virtual void GenerateNamedType(INamedTypeSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        public virtual void GenerateMethod(IMethodSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        public virtual void GenerateField(IFieldSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        public virtual void GenerateEvent(IEventSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        public virtual void GenerateProperty(IPropertySymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
        }

        internal string AddReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolVisitorAdapter adapter)
        {
            var id = VisitorHelper.GetId(symbol);

            ReferenceItem reference = new ReferenceItem();
            reference.Parts = new SortedList<SyntaxLanguage, List<LinkItem>>();
            reference.IsDefinition = symbol.IsDefinition;
            GenerateReferenceInternal(symbol, reference, adapter);

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

        internal string AddReference(string id, Dictionary<string, ReferenceItem> references)
        {
            ReferenceItem reference;
            if (!references.TryGetValue(id, out reference))
            {
                // Add id to reference dictionary
                references[id] = new ReferenceItem();
            }

            return id;
        }

        internal string AddSpecReference(
            ISymbol symbol,
            IReadOnlyList<string> typeGenericParameters,
            IReadOnlyList<string> methodGenericParameters,
            Dictionary<string, ReferenceItem> references,
            SymbolVisitorAdapter adapter,
            bool useDef = false)
        {
            var id = useDef ? VisitorHelper.GetId(symbol) : SpecIdHelper.GetSpecId(symbol, typeGenericParameters, methodGenericParameters);
            ReferenceItem reference = new ReferenceItem();
            reference.Parts = new SortedList<SyntaxLanguage, List<LinkItem>>();
            GenerateReferenceInternal(symbol, reference, adapter);
            reference.IsDefinition = symbol.IsDefinition;

            if (!symbol.IsDefinition)
            {
                var def = symbol.OriginalDefinition;
                var typeParameters = def.Accept(TypeGenericParameterNameVisitor.Instance);
                reference.Definition = AddSpecReference(def, typeParameters, null, references, adapter, true);
            }

            reference.Parent = GetReferenceParent(symbol, typeGenericParameters, methodGenericParameters, references, adapter);

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
                        return AddSpecReference(parentSymbol, typeGenericParameters, methodGenericParameters, references, adapter); ;
                    }
                default:
                    return null;
            }
        }

        internal abstract void GenerateReferenceInternal(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter);

        internal abstract void GenerateSyntax(MemberType type, ISymbol symbol, SyntaxDetail syntax, SymbolVisitorAdapter adapter);
    }
}
