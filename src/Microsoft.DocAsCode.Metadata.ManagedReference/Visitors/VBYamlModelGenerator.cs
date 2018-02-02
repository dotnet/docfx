// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class VBYamlModelGenerator : SimpleYamlModelGenerator
    {
        #region Fields
        private static readonly Regex EndRegex = new Regex(@"\s+End\s*\w*\s*$", RegexOptions.Compiled);
        #endregion

        public VBYamlModelGenerator() : base(SyntaxLanguage.VB)
        {
        }

        #region Overrides

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.VB] = NameVisitorCreator.GetVB(NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayNamesWithType[SyntaxLanguage.VB] = NameVisitorCreator.GetVB(NameOptions.WithType | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayQualifiedNames[SyntaxLanguage.VB] = NameVisitorCreator.GetVB(NameOptions.Qualified | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
        }

        public override void GenerateNamedType(INamedTypeSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateNamedType(symbol, item, adapter);

            var modifiers = new List<string>();
            var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (visiblity != null)
            {
                modifiers.Add(visiblity);
            }
            if (symbol.TypeKind == TypeKind.Class)
            {
                if (symbol.IsAbstract)
                {
                    modifiers.Add("MustInherit");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("NotInheritable");
                }
            }
            switch (symbol.TypeKind)
            {
                case TypeKind.Module:
                    modifiers.Add("Module");
                    break;
                case TypeKind.Class:
                    if (symbol.IsStatic)
                    {
                        modifiers.Add("Module");
                    }
                    else
                    {
                        modifiers.Add("Class");
                    }
                    break;
                case TypeKind.Delegate:
                    modifiers.Add("Delegate");
                    break;
                case TypeKind.Enum:
                    modifiers.Add("Enum");
                    break;
                case TypeKind.Interface:
                    modifiers.Add("Interface");
                    break;
                case TypeKind.Struct:
                    modifiers.Add("Structure");
                    break;
                default:
                    break;
            }
            item.Modifiers[SyntaxLanguage.VB] = modifiers;
        }

        public override void GenerateMethod(IMethodSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateMethod(symbol, item, adapter);

            var modifiers = new List<string>();
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
                if (visiblity != null)
                {
                    modifiers.Add(visiblity);
                }
                if (symbol.IsStatic)
                {
                    modifiers.Add("Shared");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("MustOverride");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("Overrides");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("Overridable");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("NotOverridable");
                }
            }
            item.Modifiers[SyntaxLanguage.VB] = modifiers;
        }

        public override void GenerateField(IFieldSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateField(symbol, item, adapter);

            var modifiers = new List<string>();
            var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (visiblity != null)
            {
                modifiers.Add(visiblity);
            }
            if (symbol.IsConst)
            {
                modifiers.Add("Const");
            }
            else if (symbol.IsStatic)
            {
                modifiers.Add("Shared");
            }
            if (symbol.IsReadOnly)
            {
                modifiers.Add("ReadOnly");
            }
            if (symbol.IsVolatile)
            {
                // no modifier for volatile in vb
            }
            item.Modifiers[SyntaxLanguage.VB] = modifiers;
        }

        public override void GenerateProperty(IPropertySymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateProperty(symbol, item, adapter);

            var modifiers = new List<string>();
            var propertyVisiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                if (propertyVisiblity != null)
                {
                    modifiers.Add(propertyVisiblity);
                }
                if (symbol.IsStatic)
                {
                    modifiers.Add("Shared");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("MustOverride");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("Overrides");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("Overridable");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("NotOverridable");
                }
            }
            bool hasGetMethod = symbol.GetMethod != null;
            bool hasSetMethod = symbol.SetMethod != null;
            var getMethodVisiblity = hasGetMethod ? GetVisiblity(symbol.GetMethod.DeclaredAccessibility) : null;
            var setMethodVisiblity = hasSetMethod ? GetVisiblity(symbol.SetMethod.DeclaredAccessibility) : null;
            if (hasGetMethod ^ hasSetMethod)
            {
                if (hasGetMethod)
                {
                    modifiers.Add("ReadOnly");
                }
                else
                {
                    modifiers.Add("WriteOnly");
                }
            }
            else if (propertyVisiblity != null &&
                (getMethodVisiblity == null ^ setMethodVisiblity == null))
            {
                if (setMethodVisiblity == null)
                {
                    modifiers.Add("ReadOnly");
                }
                if (getMethodVisiblity == null)
                {
                    modifiers.Add("WriteOnly");
                }
            }
            else if (getMethodVisiblity != propertyVisiblity ||
                setMethodVisiblity != propertyVisiblity)
            {
                if (getMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{getMethodVisiblity} Get");
                }
                else
                {
                    modifiers.Add("Get");
                }
                if (setMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{setMethodVisiblity} Set");
                }
                else
                {
                    modifiers.Add("Set");
                }
            }
            item.Modifiers[SyntaxLanguage.VB] = modifiers;
        }

        public override void GenerateEvent(IEventSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateEvent(symbol, item, adapter);

            var modifiers = new List<string>();
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
                if (visiblity != null)
                {
                    modifiers.Add(visiblity);
                }
                if (symbol.IsStatic)
                {
                    modifiers.Add("Shared");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("MustOverride");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("Overrides");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("Overridable");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("NotOverridable");
                }
            }
            item.Modifiers[SyntaxLanguage.VB] = modifiers;
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            switch (typeKind)
            {
                case MemberType.Class:
                    return GetClassSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Enum:
                    return GetEnumSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Interface:
                    return GetInterfaceSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Struct:
                    return GetStructSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Delegate:
                    return GetDelegateSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Method:
                    return GetMethodSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Operator:
                    return GetOperatorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Constructor:
                    return GetConstructorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Field:
                    return GetFieldSyntax((IFieldSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Event:
                    return GetEventSyntax((IEventSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Property:
                    return GetPropertySyntax((IPropertySymbol)symbol, adapter.FilterVisitor);
                default:
                    return null;
            }
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            symbol.Accept(new VBReferenceItemVisitor(reference, asOverload));
        }

        #endregion

        #region Private Methods

        #region Syntax

        private string GetClassSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            string syntaxStr;
            if (symbol.TypeKind == TypeKind.Module || symbol.IsStatic)
            {
                syntaxStr = SyntaxFactory.ModuleBlock(
                    SyntaxFactory.ModuleStatement(
                        GetAttributes(symbol, filterVisitor),
                        SyntaxFactory.TokenList(
                            GetTypeModifiers(symbol)
                        ),
                        SyntaxFactory.Identifier(symbol.Name),
                        GetTypeParameters(symbol)
                    ),
                    GetInheritsList(symbol),
                    GetImplementsList(symbol),
                    new SyntaxList<StatementSyntax>()
                ).NormalizeWhitespace().ToString();
            }
            else
            {
                syntaxStr = SyntaxFactory.ClassBlock(
                    SyntaxFactory.ClassStatement(
                        GetAttributes(symbol, filterVisitor),
                        SyntaxFactory.TokenList(
                            GetTypeModifiers(symbol)
                        ),
                        SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                        SyntaxFactory.Identifier(symbol.Name),
                        GetTypeParameters(symbol)
                    ),
                    GetInheritsList(symbol),
                    GetImplementsList(symbol),
                    new SyntaxList<StatementSyntax>(),
                    SyntaxFactory.EndClassStatement()
                ).NormalizeWhitespace().ToString();
            }
            return RemoveEnd(syntaxStr);
        }

        private string GetEnumSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            var syntaxStr = SyntaxFactory.EnumBlock(
                SyntaxFactory.EnumStatement(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Token(SyntaxKind.EnumKeyword),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetEnumUnderlyingType(symbol)
                )
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetInterfaceSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            var syntaxStr = SyntaxFactory.InterfaceBlock(
                SyntaxFactory.InterfaceStatement(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Token(SyntaxKind.InterfaceKeyword),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol)
                ),
                GetInheritsList(symbol),
                new SyntaxList<ImplementsStatementSyntax>(),
                new SyntaxList<StatementSyntax>(),
                SyntaxFactory.EndInterfaceStatement()
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetStructSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            string syntaxStr = SyntaxFactory.StructureBlock(
                SyntaxFactory.StructureStatement(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Token(SyntaxKind.StructureKeyword),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol)
                ),
                new SyntaxList<InheritsStatementSyntax>(),
                GetImplementsList(symbol),
                new SyntaxList<StatementSyntax>(),
                SyntaxFactory.EndStructureStatement()
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetDelegateSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            string syntaxStr = SyntaxFactory.DelegateStatement(
                symbol.DelegateInvokeMethod.ReturnsVoid ?
                    SyntaxKind.DelegateSubStatement :
                    SyntaxKind.DelegateFunctionStatement,
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetTypeModifiers(symbol)
                ),
                symbol.DelegateInvokeMethod.ReturnsVoid ?
                    SyntaxFactory.Token(SyntaxKind.SubKeyword) :
                    SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                SyntaxFactory.Identifier(symbol.Name),
                GetTypeParameters(symbol),
                GetParamerterList(symbol.DelegateInvokeMethod),
                GetReturnAsClause(symbol.DelegateInvokeMethod)
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetMethodSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            string syntaxStr = SyntaxFactory.MethodStatement(
                symbol.ReturnsVoid ?
                    SyntaxKind.SubStatement :
                    SyntaxKind.FunctionStatement,
                GetAttributes(symbol, filterVisitor, isExtensionMethod: symbol.IsExtensionMethod),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                symbol.ReturnsVoid ?
                    SyntaxFactory.Token(SyntaxKind.SubKeyword) :
                    SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                SyntaxFactory.Identifier(symbol.Name),
                GetTypeParameters(symbol),
                GetParamerterList(symbol),
                GetReturnAsClause(symbol),
                null,
                GetImplementsClause(symbol, filterVisitor)
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetOperatorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            var operatorToken = GetOperatorToken(symbol);
            if (operatorToken == null)
            {
                return "VB cannot support this operator.";
            }
            return SyntaxFactory.OperatorStatement(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                operatorToken.Value,
                GetParamerterList(symbol),
                GetReturnAsClause(symbol)
            ).NormalizeWhitespace().ToString();
        }

        private string GetConstructorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            var syntaxStr = SyntaxFactory.SubNewStatement(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                GetParamerterList(symbol)
            ).NormalizeWhitespace().ToString();
            return RemoveEnd(syntaxStr);
        }

        private string GetFieldSyntax(IFieldSymbol symbol, IFilterVisitor filterVisitor)
        {
            string syntaxStr;
            if (symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                syntaxStr = SyntaxFactory.EnumMemberDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetDefaultValue(symbol)
                ).NormalizeWhitespace().ToString();
            }
            else
            {
                syntaxStr = SyntaxFactory.FieldDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.ModifiedIdentifier(symbol.Name)
                            ),
                            SyntaxFactory.SimpleAsClause(
                                GetTypeSyntax(symbol.Type)
                            ),
                            GetDefaultValue(symbol)
                        )
                    )
                ).NormalizeWhitespace().ToString();
            }
            return RemoveEnd(syntaxStr);
        }

        private string GetEventSyntax(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            return SyntaxFactory.EventStatement(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                SyntaxFactory.Identifier(symbol.Name),
                null,
                SyntaxFactory.SimpleAsClause(
                    GetTypeSyntax(symbol.Type)
                ),
                GetImplementsClause(symbol, filterVisitor)
            ).NormalizeWhitespace().ToString();
        }

        private string GetPropertySyntax(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            return SyntaxFactory.PropertyStatement(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                SyntaxFactory.Identifier(symbol.MetadataName),
                GetParamerterList(symbol),
                SyntaxFactory.SimpleAsClause(
                    GetTypeSyntax(symbol.Type)
                ),
                null,
                GetImplementsClause(symbol, filterVisitor)
            ).NormalizeWhitespace().ToString();
        }

        #endregion

        private static SyntaxList<AttributeListSyntax> GetAttributes(ISymbol symbol, IFilterVisitor filterVisitor, bool inOneLine = false, bool isExtensionMethod = false)
        {
            var attrs = symbol.GetAttributes();
            List<AttributeSyntax> attrList = null;
            if (attrs.Length > 0)
            {
                attrList = (from attr in attrs
                            where !(attr.AttributeClass is IErrorTypeSymbol)
                            where attr?.AttributeConstructor != null
                            where filterVisitor.CanVisitAttribute(attr.AttributeConstructor)
                            select GetAttributeSyntax(attr)).ToList();
            }
            if (isExtensionMethod)
            {
                attrList = attrList ?? new List<AttributeSyntax>();
                attrList.Add(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName(nameof(System.Runtime.CompilerServices.ExtensionAttribute))));
            }
            if (attrList?.Count > 0)
            {
                if (inOneLine)
                {
                    return SyntaxFactory.SingletonList(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SeparatedList(attrList)));
                }
                return SyntaxFactory.List(
                    from attr in attrList
                    select SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(attr)));
            }
            return new SyntaxList<AttributeListSyntax>();
        }

        private static AttributeSyntax GetAttributeSyntax(AttributeData attr)
        {
            var attrTypeName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(attr.AttributeClass);
            if (attrTypeName.EndsWith(nameof(Attribute)))
            {
                attrTypeName = attrTypeName.Remove(attrTypeName.Length - nameof(Attribute).Length);
            }
            if (attr.ConstructorArguments.Length == 0 && attr.NamedArguments.Length == 0)
            {
                return SyntaxFactory.Attribute(SyntaxFactory.ParseName(attrTypeName));
            }
            return SyntaxFactory.Attribute(
                null,
                SyntaxFactory.ParseName(attrTypeName),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        (from item in attr.ConstructorArguments
                         select GetLiteralExpression(item) into expr
                         where expr != null
                         select (ArgumentSyntax)SyntaxFactory.SimpleArgument(expr)
                        ).Concat(
                            from item in attr.NamedArguments
                            let expr = GetLiteralExpression(item.Value)
                            where expr != null
                            select (ArgumentSyntax)SyntaxFactory.SimpleArgument(
                                SyntaxFactory.NameColonEquals(
                                    SyntaxFactory.IdentifierName(item.Key)
                                ),
                                expr
                            )
                        )
                    )
                )
            );
        }

        private static IEnumerable<SyntaxToken> GetTypeModifiers(INamedTypeSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrFriend:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                default:
                    break;
            }
            if (symbol.TypeKind == TypeKind.Class)
            {
                if (symbol.IsAbstract && symbol.IsSealed)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword);
                }
                else
                {
                    if (symbol.IsAbstract)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.MustInheritKeyword);
                    }
                    if (symbol.IsSealed)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword);
                    }
                }
            }
        }

        private IEnumerable<SyntaxToken> GetMemberModifiers(IMethodSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrFriend:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SharedKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridableKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridesKeyword);
            }
            if (symbol.MethodKind == MethodKind.Conversion)
            {
                if (symbol.Name == "op_Implicit")
                {
                    yield return SyntaxFactory.Token(SyntaxKind.WideningKeyword);
                }
                else if (symbol.Name == "op_Explicit")
                {
                    yield return SyntaxFactory.Token(SyntaxKind.NarrowingKeyword);
                }
            }
        }

        private IEnumerable<SyntaxToken> GetMemberModifiers(IFieldSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrFriend:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsConst)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ConstKeyword);
            }
            else
            {
                if (symbol.IsStatic)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.SharedKeyword);
                }
                if (symbol.IsReadOnly)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                }
            }
        }

        private IEnumerable<SyntaxToken> GetMemberModifiers(IEventSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrFriend:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SharedKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridableKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridesKeyword);
            }
        }

        private IEnumerable<SyntaxToken> GetMemberModifiers(IPropertySymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrFriend:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SharedKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridableKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverridesKeyword);
            }
            if (symbol.IsReadOnly || symbol.SetMethod == null)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
            }
            else
            {
                switch (symbol.SetMethod.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                        yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsWriteOnly || symbol.GetMethod == null)
            {
                yield return SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword);
            }
            else
            {
                switch (symbol.GetMethod.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                        yield return SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword);
                        break;
                    default:
                        break;
                }
            }
        }

        private static TypeParameterListSyntax GetTypeParameters(INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(
                    from ITypeParameterSymbol t in symbol.TypeArguments
                    select SyntaxFactory.TypeParameter(
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name),
                        GetTypeParameterConstraintClauseSyntax(t))));
        }

        private static TypeParameterListSyntax GetTypeParameters(IMethodSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(
                    from ITypeParameterSymbol t in symbol.TypeArguments
                    select SyntaxFactory.TypeParameter(
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name),
                        GetTypeParameterConstraintClauseSyntax(t))));
        }

        private static SyntaxToken GetVarianceToken(ITypeParameterSymbol t)
        {
            if (t.Variance == VarianceKind.In)
                return SyntaxFactory.Token(SyntaxKind.InKeyword);
            if (t.Variance == VarianceKind.Out)
                return SyntaxFactory.Token(SyntaxKind.OutKeyword);
            return new SyntaxToken();
        }

        private static TypeParameterConstraintClauseSyntax GetTypeParameterConstraintClauseSyntax(ITypeParameterSymbol symbol)
        {
            var contraints = GetConstraintSyntaxes(symbol).ToList();
            if (contraints.Count == 0)
            {
                return null;
            }
            if (contraints.Count == 1)
            {
                return SyntaxFactory.TypeParameterSingleConstraintClause(contraints[0]);
            }
            return SyntaxFactory.TypeParameterMultipleConstraintClause(contraints.ToArray());
        }

        private static IEnumerable<ConstraintSyntax> GetConstraintSyntaxes(ITypeParameterSymbol symbol)
        {
            if (symbol.HasReferenceTypeConstraint)
            {
                yield return SyntaxFactory.ClassConstraint(SyntaxFactory.Token(SyntaxKind.ClassKeyword));
            }
            if (symbol.HasValueTypeConstraint)
            {
                yield return SyntaxFactory.StructureConstraint(SyntaxFactory.Token(SyntaxKind.StructureKeyword));
            }
            if (symbol.ConstraintTypes.Length > 0)
            {
                for (int i = 0; i < symbol.ConstraintTypes.Length; i++)
                {
                    yield return SyntaxFactory.TypeConstraint(GetTypeSyntax(symbol.ConstraintTypes[i]));
                }
            }
            if (symbol.HasConstructorConstraint)
            {
                yield return SyntaxFactory.NewConstraint(SyntaxFactory.Token(SyntaxKind.NewKeyword));
            }
        }

        private SyntaxList<InheritsStatementSyntax> GetInheritsList(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Class && symbol.BaseType != null && symbol.BaseType.GetDocumentationCommentId() != "T:System.Object")
            {
                return SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(GetTypeSyntax(symbol.BaseType)));
            }
            if (symbol.TypeKind == TypeKind.Interface && symbol.Interfaces.Length > 0)
            {
                return SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(
                    (from t in symbol.Interfaces
                     select GetTypeSyntax(t)).ToArray()));
            }
            return new SyntaxList<InheritsStatementSyntax>();
        }

        private SyntaxList<ImplementsStatementSyntax> GetImplementsList(INamedTypeSymbol symbol)
        {
            if (symbol.AllInterfaces.Any())
            {
                return SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(
                    (from t in symbol.AllInterfaces
                     select GetTypeSyntax(t)).ToArray()));
            }
            return new SyntaxList<ImplementsStatementSyntax>();
        }

        private AsClauseSyntax GetEnumUnderlyingType(INamedTypeSymbol symbol)
        {
            if (symbol.EnumUnderlyingType.GetDocumentationCommentId() == "T:System.Int32")
            {
                return null;
            }
            return SyntaxFactory.SimpleAsClause(GetTypeSyntax(symbol.EnumUnderlyingType));
        }

        private ParameterListSyntax GetParamerterList(IMethodSymbol symbol)
        {
            if (symbol.Parameters.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    from p in symbol.Parameters
                    select SyntaxFactory.Parameter(
                        new SyntaxList<AttributeListSyntax>(),
                        SyntaxFactory.TokenList(GetParameterModifiers(p)),
                        SyntaxFactory.ModifiedIdentifier(p.Name),
                        SyntaxFactory.SimpleAsClause(GetTypeSyntax(p.Type)),
                        GetDefaultValue(p))));
        }

        private ParameterListSyntax GetParamerterList(IPropertySymbol symbol)
        {
            if (symbol.Parameters.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    from p in symbol.Parameters
                    select SyntaxFactory.Parameter(
                        new SyntaxList<AttributeListSyntax>(),
                        SyntaxFactory.TokenList(GetParameterModifiers(p)),
                        SyntaxFactory.ModifiedIdentifier(p.Name),
                        SyntaxFactory.SimpleAsClause(GetTypeSyntax(p.Type)),
                        GetDefaultValue(p))));
        }

        private IEnumerable<SyntaxToken> GetParameterModifiers(IParameterSymbol symbol)
        {
            if (symbol.RefKind == RefKind.None)
            {
            }
            else
            {
                yield return SyntaxFactory.Token(SyntaxKind.ByRefKeyword);
            }
            if (symbol.IsParams)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ParamArrayKeyword);
            }
        }

        private ImplementsClauseSyntax GetImplementsClause(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where filterVisitor.CanVisitApi(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), SyntaxFactory.IdentifierName(eii.Name))).ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.ImplementsClause(SyntaxFactory.SeparatedList(list.ToArray()));
        }

        private ImplementsClauseSyntax GetImplementsClause(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where filterVisitor.CanVisitApi(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), SyntaxFactory.IdentifierName(eii.Name))).ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.ImplementsClause(SyntaxFactory.SeparatedList(list.ToArray()));
        }

        private ImplementsClauseSyntax GetImplementsClause(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where filterVisitor.CanVisitApi(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), (SimpleNameSyntax)SyntaxFactory.IdentifierName(eii.Name))).ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.ImplementsClause(SyntaxFactory.SeparatedList(list.ToArray()));
        }

        private EqualsValueSyntax GetDefaultValue(IParameterSymbol symbol)
        {
            if (symbol.HasExplicitDefaultValue)
            {
                return GetDefaultValueCore(symbol.ExplicitDefaultValue, symbol.Type);
            }
            return null;
        }

        private EqualsValueSyntax GetDefaultValue(IFieldSymbol symbol)
        {
            if (symbol.IsConst)
            {
                if (symbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    return GetDefaultValueCore(symbol.ConstantValue, ((INamedTypeSymbol)symbol.Type).EnumUnderlyingType);
                }
                return GetDefaultValueCore(symbol.ConstantValue, symbol.Type);
            }
            return null;
        }

        //private EqualsValueSyntax GetDefaultValueCore(object value)
        //{
        //    if (value == null)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NothingLiteralExpression,
        //                SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
        //    }
        //    if (value is bool)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                (bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression,
        //                SyntaxFactory.Token(
        //                    (bool)value ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword)));
        //    }
        //    if (value is long)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((long)value)));
        //    }
        //    if (value is ulong)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((ulong)value)));
        //    }
        //    if (value is int)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((int)value)));
        //    }
        //    if (value is uint)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((uint)value)));
        //    }
        //    if (value is short)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((short)value)));
        //    }
        //    if (value is ushort)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((ushort)value)));
        //    }
        //    if (value is byte)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((byte)value)));
        //    }
        //    if (value is sbyte)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((sbyte)value)));
        //    }
        //    if (value is double)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((double)value)));
        //    }
        //    if (value is float)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((float)value)));
        //    }
        //    if (value is decimal)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.NumericLiteralExpression,
        //                SyntaxFactory.Literal((decimal)value)));
        //    }
        //    if (value is char)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.CharacterLiteralExpression,
        //                SyntaxFactory.Literal((char)value)));
        //    }
        //    if (value is string)
        //    {
        //        return SyntaxFactory.EqualsValue(
        //            SyntaxFactory.LiteralExpression(
        //                SyntaxKind.StringLiteralExpression,
        //                SyntaxFactory.Literal((string)value)));
        //    }

        //    Debug.Fail("Unknown default value!");
        //    return null;
        //}

        private static EqualsValueSyntax GetDefaultValueCore(object value, ITypeSymbol type)
        {
            var expr = GetLiteralExpression(value, type);
            if (expr != null)
            {
                return SyntaxFactory.EqualsValue(expr);
            }
            return null;
        }

        private static ExpressionSyntax GetLiteralExpression(TypedConstant constant)
        {
            if (constant.Type.TypeKind == TypeKind.Array)
            {
                if (constant.Values == null)
                {
                    return GetLiteralExpression(null, constant.Type);
                }
                var items = (from value in constant.Values
                             select GetLiteralExpression(value)).ToList();
                if (items.TrueForAll(x => x != null))
                {
                    return SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword),
                        default(SyntaxList<AttributeListSyntax>),
                        GetTypeSyntax(
                            ((IArrayTypeSymbol)constant.Type).ElementType
                        ),
                        null,
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier()
                        ),
                        SyntaxFactory.CollectionInitializer(
                            SyntaxFactory.SeparatedList(
                                from value in constant.Values
                                select GetLiteralExpression(value)
                            )
                        )
                    );
                }
                return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.Token(SyntaxKind.NewKeyword),
                    default(SyntaxList<AttributeListSyntax>),
                    GetTypeSyntax(
                        ((IArrayTypeSymbol)constant.Type).ElementType
                    ),
                    null,
                    new SyntaxList<ArrayRankSpecifierSyntax>(),
                    SyntaxFactory.CollectionInitializer()
                );
            }

            var expr = GetLiteralExpression(constant.Value, constant.Type);
            if (expr == null)
            {
                return null;
            }

            switch (constant.Type.SpecialType)
            {
                case SpecialType.System_SByte:
                    return SyntaxFactory.CTypeExpression(
                        expr,
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.SByteKeyword)));
                case SpecialType.System_Byte:
                    return SyntaxFactory.CTypeExpression(
                        expr,
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ByteKeyword)));
                case SpecialType.System_Int16:
                    return SyntaxFactory.CTypeExpression(
                        expr,
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ShortKeyword)));
                case SpecialType.System_UInt16:
                    return SyntaxFactory.CTypeExpression(
                        expr,
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.UShortKeyword)));
                default:
                    return expr;
            }
        }

        private static ExpressionSyntax GetLiteralExpression(object value, ITypeSymbol type)
        {
            if (value == null)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NothingLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.NothingKeyword));
            }
            var result = GetLiteralExpressionCore(value, type);
            if (result != null)
            {
                return result;
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                var namedType = (INamedTypeSymbol)type;
                var enumType = GetTypeSyntax(namedType);
                var isFlags = namedType.GetAttributes().Any(attr => attr.AttributeClass.GetDocumentationCommentId() == "T:System.FlagsAttribute");

                var pairs = from member in namedType.GetMembers().OfType<IFieldSymbol>()
                            where member.IsConst && member.HasConstantValue
                            select new { member.Name, member.ConstantValue };
                if (isFlags)
                {
                    var exprs = (from pair in pairs
                                 where HasFlag(namedType.EnumUnderlyingType, value, pair.ConstantValue)
                                 select SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     enumType,
                                     SyntaxFactory.Token(SyntaxKind.DotToken),
                                     SyntaxFactory.IdentifierName(pair.Name))).ToList();
                    if (exprs.Count > 0)
                    {
                        return exprs.Aggregate<ExpressionSyntax>((x, y) =>
                            SyntaxFactory.OrExpression(x, y));
                    }
                }
                else
                {
                    var expr = (from pair in pairs
                                where object.Equals(value, pair.ConstantValue)
                                select SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    enumType,
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(pair.Name))).FirstOrDefault();
                    if (expr != null)
                    {
                        return expr;
                    }
                }
                return SyntaxFactory.CTypeExpression(
                    GetLiteralExpressionCore(
                        value,
                        namedType.EnumUnderlyingType),
                    enumType);
            }
            if (value is ITypeSymbol)
            {
                return SyntaxFactory.GetTypeExpression(
                    GetTypeSyntax((ITypeSymbol)value));
            }
            Debug.Fail("Unknown default value!");
            return null;
        }

        private static bool HasFlag(ITypeSymbol type, object value, object constantValue)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                    {
                        var v = (sbyte)value;
                        var cv = (sbyte)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Byte:
                    {
                        var v = (byte)value;
                        var cv = (byte)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int16:
                    {
                        var v = (short)value;
                        var cv = (short)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt16:
                    {
                        var v = (ushort)value;
                        var cv = (ushort)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int32:
                    {
                        var v = (int)value;
                        var cv = (int)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt32:
                    {
                        var v = (uint)value;
                        var cv = (uint)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int64:
                    {
                        var v = (long)value;
                        var cv = (long)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt64:
                    {
                        var v = (ulong)value;
                        var cv = (ulong)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                default:
                    return false;
            }
        }

        private static ExpressionSyntax GetLiteralExpressionCore(object value, ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    if ((bool)value)
                    {
                        return SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword));
                    }
                    else
                    {
                        return SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword));
                    }
                case SpecialType.System_Char:
                    var ch = (char)value;
                    var category = char.GetUnicodeCategory(ch);
                    switch (category)
                    {
                        case System.Globalization.UnicodeCategory.Surrogate:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal("\"\\u" + ((int)ch).ToString("X4") + "\"c", ch));
                        default:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal((char)value));
                    }
                case SpecialType.System_SByte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((sbyte)value));
                case SpecialType.System_Byte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((byte)value));
                case SpecialType.System_Int16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((short)value));
                case SpecialType.System_UInt16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ushort)value));
                case SpecialType.System_Int32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((int)value));
                case SpecialType.System_UInt32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((uint)value));
                case SpecialType.System_Int64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((long)value));
                case SpecialType.System_UInt64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ulong)value));
                case SpecialType.System_Decimal:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((decimal)value));
                case SpecialType.System_Single:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((float)value));
                case SpecialType.System_Double:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((double)value));
                case SpecialType.System_String:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal((string)value));
                default:
                    return null;
            }
        }

        private SimpleAsClauseSyntax GetReturnAsClause(IMethodSymbol symbol)
        {
            if (symbol.ReturnsVoid)
            {
                return null;
            }
            return SyntaxFactory.SimpleAsClause(GetTypeSyntax(symbol.ReturnType));
        }

        private static SyntaxToken? GetOperatorToken(IMethodSymbol symbol)
        {
            switch (symbol.Name)
            {
                // unary
                case "op_UnaryPlus": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_UnaryNegation": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_OnesComplement": return SyntaxFactory.Token(SyntaxKind.NotKeyword);
                case "op_True": return SyntaxFactory.Token(SyntaxKind.IsTrueKeyword);
                case "op_False": return SyntaxFactory.Token(SyntaxKind.IsFalseKeyword);
                // binary
                case "op_Addition": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_Subtraction": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_Multiply": return SyntaxFactory.Token(SyntaxKind.AsteriskToken);
                case "op_Division": return SyntaxFactory.Token(SyntaxKind.SlashToken);
                case "op_Modulus": return SyntaxFactory.Token(SyntaxKind.ModKeyword);
                case "op_BitwiseAnd": return SyntaxFactory.Token(SyntaxKind.AndKeyword);
                case "op_BitwiseOr": return SyntaxFactory.Token(SyntaxKind.OrKeyword);
                case "op_ExclusiveOr": return SyntaxFactory.Token(SyntaxKind.XorKeyword);
                case "op_RightShift": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken);
                case "op_LeftShift": return SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken);
                // comparision
                case "op_Equality": return SyntaxFactory.Token(SyntaxKind.EqualsToken);
                case "op_Inequality": return SyntaxFactory.Token(SyntaxKind.LessThanGreaterThanToken);
                case "op_GreaterThan": return SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
                case "op_LessThan": return SyntaxFactory.Token(SyntaxKind.LessThanToken);
                case "op_GreaterThanOrEqual": return SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
                case "op_LessThanOrEqual": return SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
                // conversion
                case "op_Implicit":
                case "op_Explicit": return SyntaxFactory.Token(SyntaxKind.CTypeKeyword);
                // not supported:
                //case "op_LogicalNot":
                //case "op_Increment":
                //case "op_Decrement":
                //case "op_Assign":
                default: return null;
            }
        }

        private static TypeSyntax GetTypeSyntax(ITypeSymbol type)
        {
            var name = NameVisitorCreator.GetVB(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(type);
            return SyntaxFactory.ParseTypeName(name);
        }

        private static SyntaxToken GetIdentifier(ITypeSymbol type)
        {
            var name = NameVisitorCreator.GetVB(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(type);
            return SyntaxFactory.Identifier(name);
        }

        private static NameSyntax GetQualifiedNameSyntax(ITypeSymbol type)
        {
            var name = NameVisitorCreator.GetVB(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(type);
            return SyntaxFactory.ParseName(name);
        }

        private static string RemoveEnd(string code)
        {
            return EndRegex.Replace(code, string.Empty);
        }

        private static string GetVisiblity(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return "Protected";
                case Accessibility.Public:
                    return "Public";
                default:
                    return null;
            }
        }

        #endregion
    }
}
