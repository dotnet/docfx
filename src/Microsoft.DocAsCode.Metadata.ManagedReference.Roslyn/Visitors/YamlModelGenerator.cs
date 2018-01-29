// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.IO;

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

            ReferenceItem reference = new ReferenceItem()
            {
                Parts = new SortedList<SyntaxLanguage, List<LinkItem>>(),
                IsDefinition = symbol.IsDefinition,
                CommentId = VisitorHelper.GetCommentId(symbol)
            };
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

        internal string AddReference(string id, string commentId, Dictionary<string, ReferenceItem> references)
        {
            if (!references.TryGetValue(id, out ReferenceItem reference))
            {
                // Add id to reference dictionary
                references[id] = new ReferenceItem() { CommentId = commentId };
            }

            return id;
        }

        internal string AddOverloadReference(ISymbol symbol, Dictionary<string, ReferenceItem> references, SymbolVisitorAdapter adapter)
        {
            string uidBody = VisitorHelper.GetOverloadIdBody(symbol);

            ReferenceItem reference = new ReferenceItem()
            {
                Parts = new SortedList<SyntaxLanguage, List<LinkItem>>(),
                IsDefinition = true,
                CommentId = "Overload:" + uidBody
            };
            GenerateReferenceInternal(symbol, reference, adapter, true);

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

        internal string AddSpecReference(
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
            ReferenceItem reference = new ReferenceItem()
            {
                Parts = new SortedList<SyntaxLanguage, List<LinkItem>>()
            };
            GenerateReferenceInternal(symbol, reference, adapter);
            var originalSymbol = symbol;
            var reducedFrom = (symbol as IMethodSymbol)?.ReducedFrom;
            if (reducedFrom != null)
            {
                originalSymbol = reducedFrom;
            }
            reference.IsDefinition = (originalSymbol == symbol) && (id == rawId) && (symbol.IsDefinition || VisitorHelper.GetId(symbol.OriginalDefinition) == rawId);

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
            return (symbol as INamespaceSymbol)?.IsGlobalNamespace == true;
        }

        internal abstract void GenerateReferenceInternal(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload = false);

        internal abstract void GenerateSyntax(MemberType type, ISymbol symbol, SyntaxDetail syntax, SymbolVisitorAdapter adapter);
    }
}
