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
            symbol.Accept(new CSReferenceItemVisitor(reference, asOverload));
        }
    }
}
