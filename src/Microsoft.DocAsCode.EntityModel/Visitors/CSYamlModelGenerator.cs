// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class CSYamlModelGenerator : SimpleYamlModelGenerator
    {
        #region Fields
        private static readonly Regex BracesRegex = new Regex(@"\s*\{(\S|\s)*", RegexOptions.Compiled);
        #endregion

        public CSYamlModelGenerator() : base(SyntaxLanguage.CSharp)
        {
        }

        #region Overrides

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayQualifiedNames[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
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
                if (symbol.IsAbstract && symbol.IsSealed)
                {
                    modifiers.Add("static");
                }
                else if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                    modifiers.Add("class");
                    break;
                case TypeKind.Delegate:
                    modifiers.Add("delegate");
                    break;
                case TypeKind.Enum:
                    modifiers.Add("enum");
                    break;
                case TypeKind.Interface:
                    modifiers.Add("interface");
                    break;
                case TypeKind.Struct:
                    modifiers.Add("struct");
                    break;
                default:
                    break;
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
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
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("virtual");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
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
                modifiers.Add("const");
            }
            else if (symbol.IsStatic)
            {
                modifiers.Add("static");
            }
            if (symbol.IsReadOnly)
            {
                modifiers.Add("readonly");
            }
            if (symbol.IsVolatile)
            {
                modifiers.Add("volatile");
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
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
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("virtual");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            if (symbol.GetMethod != null)
            {
                var getMethodVisiblity = GetVisiblity(symbol.GetMethod.DeclaredAccessibility);
                if (propertyVisiblity != null && getMethodVisiblity == null)
                {
                }
                else if (getMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{getMethodVisiblity} get");
                }
                else
                {
                    modifiers.Add("get");
                }
            }
            if (symbol.SetMethod != null)
            {
                var setMethodVisiblity = GetVisiblity(symbol.SetMethod.DeclaredAccessibility);
                if (propertyVisiblity != null && setMethodVisiblity == null)
                {
                }
                else if (setMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{setMethodVisiblity} set");
                }
                else
                {
                    modifiers.Add("set");
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
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
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("virtual");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            string syntaxStr = null;
            switch (typeKind)
            {
                case MemberType.Class:
                    {
                        syntaxStr = GetClassSyntax(symbol);
                        break;
                    }
                case MemberType.Enum:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.EnumDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                            SyntaxFactory.Identifier(typeSymbol.Name),
                            GetEnumBaseTypeList(typeSymbol),
                            new SeparatedSyntaxList<EnumMemberDeclarationSyntax>())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveBraces(syntaxStr);
                        break;
                    }
                case MemberType.Interface:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.InterfaceDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                            SyntaxFactory.Identifier(typeSymbol.Name),
                            GetTypeParameters(typeSymbol),
                            GetBaseTypeList(typeSymbol),
                            SyntaxFactory.List(GetTypeParameterConstraints(typeSymbol)),
                            new SyntaxList<MemberDeclarationSyntax>())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveBraces(syntaxStr);
                        break;
                    }
                case MemberType.Struct:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.StructDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                            SyntaxFactory.Identifier(typeSymbol.Name),
                            GetTypeParameters(typeSymbol),
                            GetBaseTypeList(typeSymbol),
                            SyntaxFactory.List(GetTypeParameterConstraints(typeSymbol)),
                            new SyntaxList<MemberDeclarationSyntax>())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveBraces(syntaxStr);
                        break;
                    }
                case MemberType.Delegate:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.DelegateDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                            GetTypeSyntax(typeSymbol.DelegateInvokeMethod.ReturnType),
                            SyntaxFactory.Identifier(typeSymbol.Name),
                            GetTypeParameters(typeSymbol),
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SeparatedList(
                                    from p in typeSymbol.DelegateInvokeMethod.Parameters
                                    select GetParameter(p))),
                            SyntaxFactory.List(GetTypeParameterConstraints(typeSymbol)))
                            .NormalizeWhitespace()
                            .ToString();
                        break;
                    }
                case MemberType.Method:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        ExplicitInterfaceSpecifierSyntax eii = null;
                        if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
                        {
                            eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(methodSymbol)));
                        }
                        syntaxStr = SyntaxFactory.MethodDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                            GetTypeSyntax(methodSymbol.ReturnType),
                            eii,
                            SyntaxFactory.Identifier(GetMemberName(methodSymbol)),
                            GetTypeParameters(methodSymbol),
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SeparatedList(
                                    from p in methodSymbol.Parameters
                                    select GetParameter(p))),
                            SyntaxFactory.List(GetTypeParameterConstraints(methodSymbol)),
                            null,
                            null)
                            .NormalizeWhitespace()
                            .ToString();
                        break;
                    }
                case MemberType.Operator:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        var operatorToken = GetOperatorToken(methodSymbol);
                        if (operatorToken == null)
                        {
                            syntaxStr = "Not supported in c#";
                        }
                        else if (operatorToken.Value.Kind() == SyntaxKind.ImplicitKeyword || operatorToken.Value.Kind() == SyntaxKind.ExplicitKeyword)
                        {
                            syntaxStr = SyntaxFactory.ConversionOperatorDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                                operatorToken.Value,
                                GetTypeSyntax(methodSymbol.ReturnType),
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SeparatedList(
                                        from p in methodSymbol.Parameters
                                        select GetParameter(p))),
                                null,
                                null)
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        else
                        {
                            syntaxStr = SyntaxFactory.OperatorDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                                GetTypeSyntax(methodSymbol.ReturnType),
                                operatorToken.Value,
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SeparatedList(
                                        from p in methodSymbol.Parameters
                                        select GetParameter(p))),
                                null,
                                null)
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        break;
                    }
                case MemberType.Constructor:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        syntaxStr = SyntaxFactory.ConstructorDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                            SyntaxFactory.Identifier(methodSymbol.ContainingType.Name),
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SeparatedList(
                                    from p in methodSymbol.Parameters
                                    select GetParameter(p))),
                            null,
                            null)
                            .NormalizeWhitespace()
                            .ToString();
                        break;
                    };
                case MemberType.Field:
                    {
                        var fieldSymbol = (IFieldSymbol)symbol;
                        if (fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
                        {
                            syntaxStr = SyntaxFactory.EnumMemberDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.Identifier(fieldSymbol.Name),
                                GetDefaultValueClause(fieldSymbol))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        else
                        {
                            syntaxStr = SyntaxFactory.FieldDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(fieldSymbol)),
                                SyntaxFactory.VariableDeclaration(
                                    GetTypeSyntax(fieldSymbol.Type),
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier(fieldSymbol.Name)))))
                                .NormalizeWhitespace()
                                .ToString()
                                .TrimEnd(';');
                        }
                        break;
                    };
                case MemberType.Event:
                    {
                        var eventSymbol = (IEventSymbol)symbol;
                        ExplicitInterfaceSpecifierSyntax eii = null;
                        if (eventSymbol.ExplicitInterfaceImplementations.Length > 0)
                        {
                            eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(eventSymbol)));
                        }
                        syntaxStr = SyntaxFactory.EventDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(eventSymbol)),
                            SyntaxFactory.Token(SyntaxKind.EventKeyword),
                            GetTypeSyntax(eventSymbol.Type),
                            eii,
                            SyntaxFactory.Identifier(GetMemberName(eventSymbol)),
                            SyntaxFactory.AccessorList())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveBraces(syntaxStr);
                        break;
                    };
                case MemberType.Property:
                    {
                        var propertySymbol = (IPropertySymbol)symbol;
                        ExplicitInterfaceSpecifierSyntax eii = null;
                        if (propertySymbol.ExplicitInterfaceImplementations.Length > 0)
                        {
                            eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(propertySymbol)));
                        }
                        if (propertySymbol.IsIndexer)
                        {
                            syntaxStr = SyntaxFactory.IndexerDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(propertySymbol)),
                                GetTypeSyntax(propertySymbol.Type),
                                eii,
                                SyntaxFactory.BracketedParameterList(
                                    SyntaxFactory.SeparatedList(
                                        from p in propertySymbol.Parameters
                                        select GetParameter(p))),
                                SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(propertySymbol))))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        else
                        {
                            syntaxStr = SyntaxFactory.PropertyDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(propertySymbol)),
                                GetTypeSyntax(propertySymbol.Type),
                                eii,
                                SyntaxFactory.Identifier(GetMemberName(propertySymbol)),
                                SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(propertySymbol))))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        break;
                    };
            }

            return syntaxStr;
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter)
        {
            symbol.Accept(new CSReferenceItemVisitor(reference));
        }

        #endregion

        #region Private methods

        private string GetClassSyntax(ISymbol symbol)
        {
            var typeSymbol = (INamedTypeSymbol)symbol;
            var result = SyntaxFactory.ClassDeclaration(
                GetAttributes(symbol),
                SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                SyntaxFactory.Identifier(typeSymbol.Name),
                GetTypeParameters(typeSymbol),
                GetBaseTypeList(typeSymbol),
                SyntaxFactory.List(GetTypeParameterConstraints(typeSymbol)),
                new SyntaxList<MemberDeclarationSyntax>())
                .NormalizeWhitespace()
                .ToString();
            result = RemoveBraces(result);
            return result;
        }

        private static SyntaxList<AttributeListSyntax> GetAttributes(ISymbol symbol)
        {
            var attrs = symbol.GetAttributes();
            if (attrs.Length > 0)
            {
                var attrList = (from attr in attrs
                                where VisitorHelper.CanVisit(attr.AttributeConstructor)
                                select GetAttributeSyntax(attr)).ToList();
                if (attrList.Count > 0)
                {
                    return SyntaxFactory.List(
                        from attr in attrList
                        select SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(attr)));
                }
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
                SyntaxFactory.ParseName(attrTypeName),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList(
                        (from item in attr.ConstructorArguments
                         select SyntaxFactory.AttributeArgument(GetLiteralExpression(item.Value))
                        ).Concat(
                            from item in attr.NamedArguments
                            select SyntaxFactory.AttributeArgument(
                                SyntaxFactory.NameEquals(
                                    SyntaxFactory.IdentifierName(item.Key)
                                ),
                                SyntaxFactory.NameColon("="),
                                GetLiteralExpression(item.Value)
                            )
                        )
                    )
                )
            );
        }

        private static string GetMemberName(IMethodSymbol symbol)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IEventSymbol symbol)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IPropertySymbol symbol)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetEiiContainerTypeName(IMethodSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IEventSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IPropertySymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (VisitorHelper.CanVisit(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static ParameterSyntax GetParameter(IParameterSymbol p)
        {
            return SyntaxFactory.Parameter(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(GetParameterModifiers(p)),
                GetTypeSyntax(p.Type),
                SyntaxFactory.Identifier(p.Name),
                GetDefaultValueClause(p));
        }

        private static IEnumerable<SyntaxToken> GetParameterModifiers(IParameterSymbol parameter)
        {
            switch (parameter.RefKind)
            {
                case RefKind.None:
                    break;
                case RefKind.Ref:
                    yield return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                    break;
                case RefKind.Out:
                    yield return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                    break;
                default:
                    break;
            }
            if (parameter.IsParams)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ParamsKeyword);
            }
        }

        private static EqualsValueClauseSyntax GetDefaultValueClause(IParameterSymbol symbol)
        {
            if (symbol.HasExplicitDefaultValue)
            {
                return GetDefaultValueClauseCore(symbol.ExplicitDefaultValue);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClause(IFieldSymbol symbol)
        {
            if (symbol.IsConst)
            {
                return GetDefaultValueClauseCore(symbol.ConstantValue);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClauseCore(object value)
        {
            var expr = GetLiteralExpression(value);
            if (expr != null)
            {
                return SyntaxFactory.EqualsValueClause(expr);
            }
            return null;
        }

        private static LiteralExpressionSyntax GetLiteralExpression(object value)
        {
            if (value == null)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.NullKeyword));
            }
            if (value is bool)
            {
                return SyntaxFactory.LiteralExpression(
                    (bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            }
            if (value is long)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((long)value));
            }
            if (value is ulong)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((ulong)value));
            }
            if (value is int)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((int)value));
            }
            if (value is uint)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((uint)value));
            }
            if (value is short)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((short)value));
            }
            if (value is ushort)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((ushort)value));
            }
            if (value is byte)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((byte)value));
            }
            if (value is sbyte)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((sbyte)value));
            }
            if (value is double)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((double)value));
            }
            if (value is float)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((float)value));
            }
            if (value is decimal)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal((decimal)value));
            }
            if (value is char)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression,
                    SyntaxFactory.Literal((char)value));
            }
            if (value is string)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal((string)value));
            }
            Debug.Fail("Unknown default value!");
            return null;
        }

        private static IEnumerable<TypeParameterConstraintClauseSyntax> GetTypeParameterConstraints(INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                yield break;
            }
            foreach (ITypeParameterSymbol ta in symbol.TypeArguments)
            {
                if (ta.HasConstructorConstraint || ta.HasReferenceTypeConstraint || ta.HasValueTypeConstraint || ta.ConstraintTypes.Length > 0)
                {
                    yield return SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(ta.Name), SyntaxFactory.SeparatedList(GetTypeParameterConstraint(ta)));
                }
            }
        }

        private static IEnumerable<TypeParameterConstraintClauseSyntax> GetTypeParameterConstraints(IMethodSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                yield break;
            }
            foreach (ITypeParameterSymbol ta in symbol.TypeArguments)
            {
                if (ta.HasConstructorConstraint || ta.HasReferenceTypeConstraint || ta.HasValueTypeConstraint || ta.ConstraintTypes.Length > 0)
                {
                    yield return SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(ta.Name), SyntaxFactory.SeparatedList(GetTypeParameterConstraint(ta)));
                }
            }
        }

        private static IEnumerable<TypeParameterConstraintSyntax> GetTypeParameterConstraint(ITypeParameterSymbol symbol)
        {
            if (symbol.HasReferenceTypeConstraint)
            {
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
            }
            if (symbol.HasValueTypeConstraint)
            {
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint);
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
                yield return SyntaxFactory.ConstructorConstraint();
            }
        }

        private BaseListSyntax GetBaseTypeList(INamedTypeSymbol symbol)
        {
            IReadOnlyList<INamedTypeSymbol> baseTypeList;
            if (symbol.TypeKind != TypeKind.Class || symbol.BaseType == null || symbol.BaseType.GetDocumentationCommentId() == "T:System.Object")
            {
                baseTypeList = symbol.AllInterfaces;
            }
            else
            {
                baseTypeList = new[] { symbol.BaseType }.Concat(symbol.AllInterfaces).ToList();
            }
            if (baseTypeList.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.BaseList(
                SyntaxFactory.SeparatedList<BaseTypeSyntax>(
                    from t in baseTypeList
                    select SyntaxFactory.SimpleBaseType(GetTypeSyntax(t))));
        }

        private BaseListSyntax GetEnumBaseTypeList(INamedTypeSymbol symbol)
        {
            var underlyingType = symbol.EnumUnderlyingType;
            if (underlyingType.GetDocumentationCommentId() == "T:System.Int32")
            {
                return null;
            }
            return SyntaxFactory.BaseList(
                SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                    SyntaxFactory.SimpleBaseType(
                        GetTypeSyntax(underlyingType))));
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
                        new SyntaxList<AttributeListSyntax>(),
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name))));
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
                        new SyntaxList<AttributeListSyntax>(),
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name))));
        }

        private static SyntaxToken GetVarianceToken(ITypeParameterSymbol t)
        {
            if (t.Variance == VarianceKind.In)
                return SyntaxFactory.Token(SyntaxKind.InKeyword);
            if (t.Variance == VarianceKind.Out)
                return SyntaxFactory.Token(SyntaxKind.OutKeyword);
            return new SyntaxToken();
        }

        private static IEnumerable<SyntaxToken> GetTypeModifiers(INamedTypeSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
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
                    yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                }
                else
                {
                    if (symbol.IsAbstract)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
                    }
                    if (symbol.IsSealed)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
                    }
                }
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IMethodSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
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
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IEventSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                    case Accessibility.Private:
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IPropertySymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                    case Accessibility.Private:
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IFieldSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                case Accessibility.Private:
                default:
                    break;
            }
            if (symbol.IsConst)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ConstKeyword);
            }
            else
            {
                if (symbol.IsStatic)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                }
                if (symbol.IsReadOnly)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                }
                if (symbol.IsVolatile)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.VolatileKeyword);
                }
            }
        }

        private static SyntaxToken? GetOperatorToken(IMethodSymbol symbol)
        {
            switch (symbol.Name)
            {
                // unary
                case "op_UnaryPlus": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_UnaryNegation": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_LogicalNot": return SyntaxFactory.Token(SyntaxKind.ExclamationToken);
                case "op_OnesComplement": return SyntaxFactory.Token(SyntaxKind.TildeToken);
                case "op_Increment": return SyntaxFactory.Token(SyntaxKind.PlusPlusToken);
                case "op_Decrement": return SyntaxFactory.Token(SyntaxKind.MinusMinusToken);
                case "op_True": return SyntaxFactory.Token(SyntaxKind.TrueKeyword);
                case "op_False": return SyntaxFactory.Token(SyntaxKind.FalseKeyword);
                // binary
                case "op_Addition": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_Subtraction": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_Multiply": return SyntaxFactory.Token(SyntaxKind.AsteriskToken);
                case "op_Division": return SyntaxFactory.Token(SyntaxKind.SlashToken);
                case "op_Modulus": return SyntaxFactory.Token(SyntaxKind.PercentToken);
                case "op_BitwiseAnd": return SyntaxFactory.Token(SyntaxKind.AmpersandToken);
                case "op_BitwiseOr": return SyntaxFactory.Token(SyntaxKind.BarToken);
                case "op_ExclusiveOr": return SyntaxFactory.Token(SyntaxKind.CaretToken);
                case "op_RightShift": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken);
                case "op_LeftShift": return SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken);
                // comparision
                case "op_Equality": return SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken);
                case "op_Inequality": return SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken);
                case "op_GreaterThan": return SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
                case "op_LessThan": return SyntaxFactory.Token(SyntaxKind.LessThanToken);
                case "op_GreaterThanOrEqual": return SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
                case "op_LessThanOrEqual": return SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
                // conversion
                case "op_Implicit": return SyntaxFactory.Token(SyntaxKind.ImplicitKeyword);
                case "op_Explicit": return SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);
                // not supported:
                //case "op_Assign":
                default: return null;
            }
        }

        private static IEnumerable<AccessorDeclarationSyntax> GetPropertyAccessors(IPropertySymbol propertySymbol)
        {
            var getAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.GetMethod, SyntaxKind.GetAccessorDeclaration, SyntaxKind.GetKeyword);
            if (getAccessor != null)
            {
                yield return getAccessor;
            }
            var setAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.SetMethod, SyntaxKind.SetAccessorDeclaration, SyntaxKind.SetKeyword);
            if (setAccessor != null)
            {
                yield return setAccessor;
            }
        }

        private static AccessorDeclarationSyntax GetPropertyAccessorCore(
            IPropertySymbol propertySymbol, IMethodSymbol methodSymbol,
            SyntaxKind kind, SyntaxKind keyword)
        {
            if (methodSymbol == null)
            {
                return null;
            }
            switch (methodSymbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    if (propertySymbol.DeclaredAccessibility == Accessibility.Protected ||
                        propertySymbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            new SyntaxList<AttributeListSyntax>(),
                            new SyntaxTokenList(),
                            SyntaxFactory.Token(keyword),
                            null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    else
                    {
                        return SyntaxFactory.AccessorDeclaration(
                            kind,
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
                            SyntaxFactory.Token(keyword),
                            null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                case Accessibility.Public:
                    return SyntaxFactory.AccessorDeclaration(kind,
                        new SyntaxList<AttributeListSyntax>(),
                        new SyntaxTokenList(),
                        SyntaxFactory.Token(keyword),
                        null,
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                default:
                    if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            new SyntaxList<AttributeListSyntax>(),
                            new SyntaxTokenList(),
                            SyntaxFactory.Token(keyword),
                            null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    return null;
            }
        }

        private static string RemoveBraces(string text)
        {
            return BracesRegex.Replace(text, string.Empty);
        }

        private static TypeSyntax GetTypeSyntax(ITypeSymbol type)
        {
            var name = NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(type);
            return SyntaxFactory.ParseTypeName(name);
        }

        private static string GetVisiblity(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return "protected";
                case Accessibility.Public:
                    return "public";
                default:
                    return null;
            }
        }

        #endregion
    }
}
