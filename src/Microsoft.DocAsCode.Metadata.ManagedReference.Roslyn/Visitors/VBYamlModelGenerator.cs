// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class VBYamlModelGenerator : SimpleYamlModelGenerator
    {
        public VBYamlModelGenerator() : base(SyntaxLanguage.VB)
        {
        }

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.VB] = SymbolFormatter.GetName(symbol, SyntaxLanguage.VB);
            item.DisplayNamesWithType[SyntaxLanguage.VB] = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.VB);
            item.DisplayQualifiedNames[SyntaxLanguage.VB] = SymbolFormatter.GetQualifiedName(symbol, SyntaxLanguage.VB);
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            return SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.VB, adapter.FilterVisitor);
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            if (!reference.NameParts.ContainsKey(SyntaxLanguage.VB))
                reference.NameParts.Add(SyntaxLanguage.VB, new());
            if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.VB))
                reference.NameWithTypeParts.Add(SyntaxLanguage.VB, new());
            if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.VB))
                reference.QualifiedNameParts.Add(SyntaxLanguage.VB, new());

            reference.NameParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.VB, asOverload);
            reference.NameWithTypeParts[SyntaxLanguage.VB] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.VB, asOverload);
            reference.QualifiedNameParts[SyntaxLanguage.VB] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.VB, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.VB, asOverload);
        }
    }
}
