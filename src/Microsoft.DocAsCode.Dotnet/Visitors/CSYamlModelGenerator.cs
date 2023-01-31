// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class CSYamlModelGenerator : SimpleYamlModelGenerator
    {
        public CSYamlModelGenerator() : base(SyntaxLanguage.CSharp)
        {
        }

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.CSharp] = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp);
            item.DisplayNamesWithType[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
            item.DisplayQualifiedNames[SyntaxLanguage.CSharp] = SymbolFormatter.GetQualifiedName(symbol, SyntaxLanguage.CSharp);
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            return SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, adapter.FilterVisitor);
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            if (!reference.NameParts.ContainsKey(SyntaxLanguage.CSharp))
                reference.NameParts.Add(SyntaxLanguage.CSharp, new());
            if (!reference.NameWithTypeParts.ContainsKey(SyntaxLanguage.CSharp))
                reference.NameWithTypeParts.Add(SyntaxLanguage.CSharp, new());
            if (!reference.QualifiedNameParts.ContainsKey(SyntaxLanguage.CSharp))
                reference.QualifiedNameParts.Add(SyntaxLanguage.CSharp, new());

            reference.NameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.CSharp, asOverload);
            reference.NameWithTypeParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.CSharp, asOverload);
            reference.QualifiedNameParts[SyntaxLanguage.CSharp] = SymbolFormatter.GetQualifiedNameParts(symbol, SyntaxLanguage.CSharp, nullableReferenceType: false, asOverload).ToLinkItems(SyntaxLanguage.CSharp, asOverload);
        }
    }
}
