// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Diagnostics;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public abstract class SimpleYamlModelGenerator : YamlModelGenerator
    {
        #region Fields
        public static readonly SymbolDisplayFormat ShortFormat =
            new SymbolDisplayFormat(
                SymbolDisplayGlobalNamespaceStyle.Omitted,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
                SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
                SymbolDisplayDelegateStyle.NameOnly,
                SymbolDisplayExtensionMethodStyle.Default,
                SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                SymbolDisplayPropertyStyle.NameOnly,
                SymbolDisplayLocalOptions.None,
                SymbolDisplayKindOptions.None,
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
        public static readonly SymbolDisplayFormat QualifiedFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;
        #endregion

        protected SimpleYamlModelGenerator(SyntaxLanguage language)
        {
            Language = language;
        }

        public SyntaxLanguage Language { get; private set; }

        protected abstract string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter);

        protected abstract void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload);

        internal override sealed void GenerateSyntax(MemberType type, ISymbol symbol, SyntaxDetail syntax, SymbolVisitorAdapter adapter)
        {
            string syntaxStr = GetSyntaxContent(type, symbol, adapter);

            Debug.Assert(!string.IsNullOrEmpty(syntaxStr));
            if (string.IsNullOrEmpty(syntaxStr)) return;

            syntax.Content[Language] = syntaxStr;
        }

        internal override sealed void GenerateReferenceInternal(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            GenerateReference(symbol, reference, adapter, asOverload);
        }

        public static CompositeYamlModelGenerator operator +(SimpleYamlModelGenerator left, SimpleYamlModelGenerator right)
        {
            return new CompositeYamlModelGenerator(left, right);
        }
    }
}
