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
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Metadata.ManagedReference.Roslyn.Helpers;

    public class CSYamlModelGenerator : SimpleYamlModelGenerator
    {
        #region Fields
        private static readonly Regex BracesRegex = new Regex(@"\s*\{(;|\s)*\}\s*$", RegexOptions.Compiled);
        #endregion

        public CSYamlModelGenerator() : base(SyntaxLanguage.CSharp)
        {
        }

        #region Overrides

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayNamesWithType[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
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
                if (symbol.IsStatic)
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
            if (symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsRefLikeType)
                {
                    modifiers.Add("ref");
                }
                if (symbol.IsReadOnly)
                {
                    modifiers.Add("readonly");
                }
            }
            switch (symbol.TypeKind)
            {
                case TypeKind.Module:
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
                if (symbol.IsExtern)
                {
                    modifiers.Add("extern");
                }
                if (symbol.IsVirtual && !symbol.IsSealed)
                {
                    modifiers.Add("virtual");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsSealed && !symbol.IsVirtual)
                {
                    modifiers.Add("sealed");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if ((symbol.ContainingType.TypeKind == TypeKind.Struct) && symbol.IsReadOnly)
                {
                    modifiers.Add("readonly");
                }
                if (symbol.IsAsync)
                {
                    modifiers.Add("async");
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
            var isPropertyReadonly = IsPropertyReadonly(symbol);
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
                if (symbol.IsVirtual && !symbol.IsSealed)
                {
                    modifiers.Add("virtual");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsSealed && !symbol.IsVirtual)
                {
                    modifiers.Add("sealed");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (isPropertyReadonly)
                {
                    modifiers.Add("readonly");
                }
            }
            if (symbol.GetMethod != null)
            {
                var getMethodVisiblity = GetVisiblity(symbol.GetMethod.DeclaredAccessibility);
                var readonlyModifier = symbol.GetMethod.IsReadOnly && !isPropertyReadonly ? "readonly " : "";
                if (propertyVisiblity != null && getMethodVisiblity == null)
                {
                }
                else if (getMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{getMethodVisiblity} {readonlyModifier}get");
                }
                else
                {
                    modifiers.Add($"{readonlyModifier}get");
                }
            }
            if (symbol.SetMethod != null)
            {
                var setMethodVisiblity = GetVisiblity(symbol.SetMethod.DeclaredAccessibility);
                var readonlyModifier = symbol.SetMethod.IsReadOnly && !isPropertyReadonly ? "readonly " : "";
                if (propertyVisiblity != null && setMethodVisiblity == null)
                {
                }
                else if (setMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{setMethodVisiblity} {readonlyModifier}set");
                }
                else
                {
                    modifiers.Add($"{readonlyModifier}set");
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
                if (symbol.IsVirtual && !symbol.IsSealed)
                {
                    modifiers.Add("virtual");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsSealed && !symbol.IsVirtual)
                {
                    modifiers.Add("sealed");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            string result;

            switch (typeKind)
            {
                case MemberType.Class:
                    result = GetClassSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Enum:
                    result = GetEnumSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Interface:
                    result = GetInterfaceSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Struct:
                    result = GetStructSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Delegate:
                    result = GetDelegateSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Method:
                    result = GetMethodSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Operator:
                    result = GetOperatorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Constructor:
                    result = GetConstructorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Field:
                    result = GetFieldSyntax((IFieldSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Event:
                    result = GetEventSyntax((IEventSymbol)symbol, adapter.FilterVisitor);
                    break;
                case MemberType.Property:
                    result = GetPropertySyntax((IPropertySymbol)symbol, adapter.FilterVisitor);
                    break;
                default:
                    return null;
            }

            if ((result != null) && result.Contains("delegate *"))
            {
                // This is the expected formatting, but isn't handled in the whitespace normalizer until C# 10
                result = Regex.Replace(result, @"delegate\s*\*", "delegate*");
                result = Regex.Replace(result, @"\*unmanaged", "* unmanaged");
                result = Regex.Replace(result, @"unmanaged\s+<", "unmanaged<");
                result = Regex.Replace(result, @"(_|\w)\s*\*(,|>)", "$1*$2");
                result = Regex.Replace(result, @">(_|\w)", "> $1");
            }
            return result;
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            symbol.Accept(new CSReferenceItemVisitor(reference, asOverload));
        }

        #endregion

        #region Private methods

        #region Syntax

        private string GetClassSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            RemoveBraces(
                SyntaxFactory.ClassDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetTypeModifiers(symbol)),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol),
                    GetBaseTypeList(symbol),
                    SyntaxFactory.List(GetTypeParameterConstraints(symbol)),
                    new SyntaxList<MemberDeclarationSyntax>()
                ).NormalizeWhitespace().ToString()
            );

        private string GetEnumSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            RemoveBraces(
                SyntaxFactory.EnumDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetTypeModifiers(symbol)),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetEnumBaseTypeList(symbol),
                    new SeparatedSyntaxList<EnumMemberDeclarationSyntax>()
                )
                .NormalizeWhitespace()
                .ToString()
            );

        private string GetInterfaceSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            RemoveBraces(
                SyntaxFactory.InterfaceDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol),
                    GetBaseTypeList(symbol),
                    SyntaxFactory.List(
                        GetTypeParameterConstraints(symbol)
                    ),
                    new SyntaxList<MemberDeclarationSyntax>()
                ).NormalizeWhitespace().ToString()
            );

        private string GetStructSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            RemoveBraces(
                SyntaxFactory.StructDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol),
                    GetBaseTypeList(symbol),
                    SyntaxFactory.List(
                        GetTypeParameterConstraints(symbol)
                    ),
                    new SyntaxList<MemberDeclarationSyntax>()
                ).NormalizeWhitespace().ToString()
            );

        private string GetDelegateSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            SyntaxFactory.DelegateDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetTypeModifiers(symbol)
                ),
                GetMethodTypeSyntax(symbol.DelegateInvokeMethod),
                SyntaxFactory.Identifier(symbol.Name),
                GetTypeParameters(symbol),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        from p in symbol.DelegateInvokeMethod.Parameters
                        select GetParameter(p, filterVisitor)
                    )
                ),
                SyntaxFactory.List(
                    GetTypeParameterConstraints(symbol)
                )
            ).NormalizeWhitespace().ToString();

        private string GetMethodSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            return SyntaxFactory.MethodDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                GetMethodTypeSyntax(symbol),
                eii,
                SyntaxFactory.Identifier(
                    GetMemberName(symbol, filterVisitor)
                ),
                GetTypeParameters(symbol),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        symbol.Parameters.Select((p, i) => GetParameter(p, filterVisitor, i == 0 && symbol.IsExtensionMethod))
                    )
                ),
                SyntaxFactory.List(
                    GetTypeParameterConstraints(symbol)
                ),
                null,
                null
            ).NormalizeWhitespace().ToString();
        }

        private string GetOperatorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            var operatorToken = GetOperatorToken(symbol);
            if (operatorToken == null)
            {
                return "Not supported in c#";
            }
            else if (operatorToken.Value.Kind() == SyntaxKind.ImplicitKeyword || operatorToken.Value.Kind() == SyntaxKind.ExplicitKeyword)
            {
                return SyntaxFactory.ConversionOperatorDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    operatorToken.Value,
                    GetMethodTypeSyntax(symbol),
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor)
                        )
                    ),
                    null,
                    null
                ).NormalizeWhitespace().ToString();
            }
            else
            {
                return SyntaxFactory.OperatorDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    GetMethodTypeSyntax(symbol),
                    operatorToken.Value,
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor)
                        )
                    ),
                    null,
                    null
                ).NormalizeWhitespace().ToString();
            }
        }

        private string GetConstructorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor) =>
            SyntaxFactory.ConstructorDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                SyntaxFactory.Identifier(symbol.ContainingType.Name),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        from p in symbol.Parameters
                        select GetParameter(p, filterVisitor)
                    )
                ),
                null,
                (BlockSyntax)null
            ).NormalizeWhitespace().ToString();

        private string GetFieldSyntax(IFieldSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                return SyntaxFactory.EnumMemberDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetDefaultValueClause(symbol)
                ).NormalizeWhitespace().ToString();
            }
            else
            {
                return SyntaxFactory.FieldDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    SyntaxFactory.VariableDeclaration(
                        GetTypeSyntax(symbol.Type),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(symbol.Name),
                                null,
                                GetDefaultValueClause(symbol)
                            )
                        )
                    )
                ).NormalizeWhitespace().ToString().TrimEnd(';');
            }
        }

        private string GetEventSyntax(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            return RemoveBraces(
                SyntaxFactory.EventDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    SyntaxFactory.Token(SyntaxKind.EventKeyword),
                    GetTypeSyntax(symbol.Type),
                    eii,
                    SyntaxFactory.Identifier(GetMemberName(symbol, filterVisitor)),
                    SyntaxFactory.AccessorList()
                ).NormalizeWhitespace().ToString()
            );
        }

        private string GetPropertySyntax(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            string result;
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            if (symbol.IsIndexer)
            {
                result = SyntaxFactory.IndexerDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    GetPropertyTypeSyntax(symbol),
                    eii,
                    SyntaxFactory.BracketedParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor))),
                    SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(symbol, filterVisitor))))
                    .NormalizeWhitespace()
                    .ToString();
            }
            else
            {
                result = SyntaxFactory.PropertyDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    GetPropertyTypeSyntax(symbol),
                    eii,
                    SyntaxFactory.Identifier(GetMemberName(symbol, filterVisitor)),
                    SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(symbol, filterVisitor))))
                    .NormalizeWhitespace()
                    .ToString();
            }

            if (result.Contains("\r\n"))
            {
                result = Regex.Replace(result, @"\s*\{\[", "\r\n{\r\n    [");
                result = Regex.Replace(result, @";\s*\[", ";\r\n    [");
                result = Regex.Replace(result, @";\s*}", ";\r\n}");
            }

            result = Regex.Replace(result, @"\s*\{\s*get;\s*set;\s*}\s*$", " { get; set; }");
            result = Regex.Replace(result, @"\s*\{\s*get;\s*}\s*$", " { get; }");
            result = Regex.Replace(result, @"\s*\{\s*set;\s*}\s*$", " { set; }");
            result = Regex.Replace(result, @"\s*\{\s*get;\s*protected set;\s*}\s*$", " { get; protected set; }");
            result = Regex.Replace(result, @"\s*\{\s*protected get;\s*set;\s*}\s*$", " { protected get; set; }");

            return result;
        }

        #endregion

        private static SyntaxList<AttributeListSyntax> GetAttributes(ISymbol symbol, IFilterVisitor filterVisitor, bool inOneLine = false)
        {
            var attrs = symbol.GetAttributes();
            if (attrs.Length > 0)
            {
                var attrList = (from attr in attrs
                                where !(attr.AttributeClass is IErrorTypeSymbol)
                                where attr?.AttributeConstructor != null
                                where filterVisitor.CanVisitAttribute(attr.AttributeConstructor)
                                select GetAttributeSyntax(attr)).ToList();
                if (attrList.Count > 0)
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
            }
            return new SyntaxList<AttributeListSyntax>();
        }

        private static AttributeSyntax GetAttributeSyntax(AttributeData attr)
        {
            var attrTypeName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(attr.AttributeClass);
            if (attrTypeName.EndsWith(nameof(Attribute), StringComparison.Ordinal))
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
                         select GetLiteralExpression(item) into expr
                         where expr != null
                         select SyntaxFactory.AttributeArgument(expr)
                        ).Concat(
                            from item in attr.NamedArguments
                            let expr = GetLiteralExpression(item.Value)
                            where expr != null
                            select SyntaxFactory.AttributeArgument(
                                SyntaxFactory.NameEquals(
                                    SyntaxFactory.IdentifierName(item.Key)
                                ),
                                null,
                                expr
                            )
                        )
                    )
                )
            );
        }

        private static string GetMemberName(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetEiiContainerTypeName(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static ParameterSyntax GetParameter(IParameterSymbol parameter, IFilterVisitor filterVisitor, bool isThisParameter = false)
        {
            return SyntaxFactory.Parameter(
                GetAttributes(parameter, filterVisitor, true),
                SyntaxFactory.TokenList(GetParameterModifiers(parameter, isThisParameter)),
                GetTypeSyntax(parameter.Type),
                SyntaxFactory.Identifier(parameter.Name),
                GetDefaultValueClause(parameter));
        }

        private static IEnumerable<SyntaxToken> GetParameterModifiers(IParameterSymbol parameter, bool isThisParameter)
        {
            if (isThisParameter)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ThisKeyword);
            }
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
                case RefKind.In:
                    yield return SyntaxFactory.Token(SyntaxKind.InKeyword);
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
                return GetDefaultValueClauseCore(symbol.ExplicitDefaultValue, symbol.Type);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClause(IFieldSymbol symbol)
        {
            if (symbol.IsConst)
            {
                if (symbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    return GetDefaultValueClauseCore(symbol.ConstantValue, ((INamedTypeSymbol)symbol.Type).EnumUnderlyingType);
                }
                return GetDefaultValueClauseCore(symbol.ConstantValue, symbol.Type);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClauseCore(object value, ITypeSymbol type)
        {
            var expr = GetLiteralExpression(value, type);
            if (expr != null)
            {
                return SyntaxFactory.EqualsValueClause(expr);
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
                        (ArrayTypeSyntax)GetTypeSyntax(constant.Type),
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ArrayInitializerExpression,
                            SyntaxFactory.SeparatedList(
                                from value in constant.Values
                                select GetLiteralExpression(value))));
                }
                return SyntaxFactory.ArrayCreationExpression(
                    (ArrayTypeSyntax)GetTypeSyntax(constant.Type));
            }

            var expr = GetLiteralExpression(constant.Value, constant.Type);
            if (expr == null)
            {
                return null;
            }

            switch (constant.Type.SpecialType)
            {
                case SpecialType.System_SByte:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.SByteKeyword)),
                        expr);
                case SpecialType.System_Byte:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ByteKeyword)),
                        expr);
                case SpecialType.System_Int16:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ShortKeyword)),
                        expr);
                case SpecialType.System_UInt16:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.UShortKeyword)),
                        expr);
                default:
                    return expr;
            }
        }

        private static ExpressionSyntax GetLiteralExpression(object value, ITypeSymbol type)
        {
            bool isNullable = type.GetDocumentationCommentId()?.StartsWith("T:System.Nullable{") == true;
            if (value == null)
            {
                if (type.IsValueType && !isNullable)
                {
                    return SyntaxFactory.DefaultExpression(GetTypeSyntax(type));
                }
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.NullKeyword));
            }
            if (isNullable)
            {
                var namedType = (INamedTypeSymbol)type;
                type = namedType.TypeArguments[0];
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
                            select (member.Name, member.ConstantValue);
                if (isFlags)
                {
                    var exprs = GetFlagExpressions(pairs, value, namedType).ToList();
                    if (exprs.Count > 0)
                    {
                        return exprs.Aggregate((x, y) =>
                                SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, x, y));
                    }
                }
                else
                {
                    var expr = (from pair in pairs
                                where object.Equals(value, pair.ConstantValue)
                                select SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    enumType,
                                    SyntaxFactory.IdentifierName(pair.Name))).FirstOrDefault();
                    if (expr != null)
                    {
                        return expr;
                    }
                }
                return SyntaxFactory.CastExpression(
                    enumType,
                    GetLiteralExpressionCore(
                        value,
                        namedType.EnumUnderlyingType));
            }
            if (value is ITypeSymbol)
            {
                return SyntaxFactory.TypeOfExpression(
                    GetTypeSyntax((ITypeSymbol)value));
            }
            Debug.Fail("Unknown default value!");
            return null;
        }

        private static IEnumerable<ExpressionSyntax> GetFlagExpressions(IEnumerable<(string Name, object ConstantValue)> flags, object value, INamedTypeSymbol namedType)
        {
            switch (namedType.EnumUnderlyingType.SpecialType)
            {
                case SpecialType.System_SByte:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (sbyte)p.ConstantValue)), (sbyte)value, namedType);
                case SpecialType.System_Byte:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (byte)p.ConstantValue)), (byte)value, namedType);
                case SpecialType.System_Int16:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (short)p.ConstantValue)), (short)value, namedType);
                case SpecialType.System_UInt16:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (ushort)p.ConstantValue)), (ushort)value, namedType);
                case SpecialType.System_Int32:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (int)p.ConstantValue)), (int)value, namedType);
                case SpecialType.System_UInt32:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (uint)p.ConstantValue)), (uint)value, namedType);
                case SpecialType.System_Int64:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (long)p.ConstantValue)), (long)value, namedType);
                case SpecialType.System_UInt64:
                    return GetFlagExpressions(flags.Select(p => (p.Name, (ulong)p.ConstantValue)), (ulong)value, namedType);
                default:
                    return Array.Empty<ExpressionSyntax>();
            }
        }

        private static IEnumerable<ExpressionSyntax> GetFlagExpressions<T>(IEnumerable<(string Name, T Value)> flags, T value, INamedTypeSymbol namedType) where T : unmanaged
        {
            var enumType = GetTypeSyntax(namedType);
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                string defaultFlagName = flags.FirstOrDefault(f => EqualityComparer<T>.Default.Equals(f.Value, default)).Name;
                return defaultFlagName != null ? new[] { GetFlagExpression(defaultFlagName) } : Array.Empty<ExpressionSyntax>();
            }
            var negativeFlags = flags.Where(p => Comparer<T>.Default.Compare(p.Value, default) < 0);
            var positiveFlags = flags.Where(p => Comparer<T>.Default.Compare(p.Value, default) > 0);
            var sortedFlags = negativeFlags.OrderByDescending(p => p.Value).Concat(positiveFlags.OrderByDescending(p => p.Value)).ToList();
            if (sortedFlags.Count == 0)
            {
                return Array.Empty<ExpressionSyntax>();
            }
            var results = new List<ExpressionSyntax>();
            foreach (var (flagName, flagValue) in sortedFlags)
            {
                if (EnumOps.HasAllFlags(value, flagValue))
                {
                    results.Add(GetFlagExpression(flagName));
                    value = EnumOps.ClearFlags(value, flagValue);
                }
            }
            results.Reverse();
            if (!EqualityComparer<T>.Default.Equals(value, default))
            {
                results.Add(SyntaxFactory.CastExpression(
                    enumType,
                    GetLiteralExpressionCore(
                        value,
                        namedType.EnumUnderlyingType)));
            }
            return results;

            ExpressionSyntax GetFlagExpression(string flagName) => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                enumType,
                SyntaxFactory.IdentifierName(flagName));
        }

        public static ExpressionSyntax GetLiteralExpressionCore(object value, ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.LiteralExpression(
                        (bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                case SpecialType.System_Char:
                    var ch = (char)value;
                    var category = char.GetUnicodeCategory(ch);
                    switch (category)
                    {
                        case System.Globalization.UnicodeCategory.Surrogate:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal("'\\u" + ((int)ch).ToString("X4") + "'", ch));
                        default:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal((char)value));
                    }
                case SpecialType.System_SByte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToSByte(value)));
                case SpecialType.System_Byte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToByte(value)));
                case SpecialType.System_Int16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToInt16(value)));
                case SpecialType.System_UInt16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToUInt16(value)));
                case SpecialType.System_Int32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToInt32(value)));
                case SpecialType.System_UInt32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToUInt32(value)));
                case SpecialType.System_Int64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToInt64(value)));
                case SpecialType.System_UInt64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToUInt64(value)));
                case SpecialType.System_IntPtr:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToInt32(value)));
                case SpecialType.System_UIntPtr:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToUInt32(value)));
                case SpecialType.System_Decimal:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToDecimal(value)));
                case SpecialType.System_Single:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToSingle(value)));
                case SpecialType.System_Double:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToDouble(value)));
                case SpecialType.System_String:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(Convert.ToString(value)));
                default:
                    return null;
            }
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
                baseTypeList = symbol.AllInterfaces.Where(s=>IsSymbolAccessible(s)).ToList();
            }
            else
            {
                baseTypeList = new[] { symbol.BaseType }.Concat(symbol.AllInterfaces.Where(s => IsSymbolAccessible(s))).ToList();
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

        private static bool IsSymbolAccessible(ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public && symbol.DeclaredAccessibility != Accessibility.Protected && symbol.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
                return false;
            if (symbol.ContainingSymbol != null && symbol.Kind == SymbolKind.NamedType)
                return IsSymbolAccessible(symbol.ContainingSymbol);
            return true;
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
                if (symbol.IsStatic)
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
            if (symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsRefLikeType)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                }

                if (symbol.IsReadOnly)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
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
            if (symbol.IsExtern)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ExternKeyword);
            }
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if ((symbol.ContainingType.TypeKind == TypeKind.Struct) && symbol.IsReadOnly)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
            }
            if (symbol.IsAsync)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
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
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
        }

        private static bool IsPropertyReadonly(IPropertySymbol property)
        {
            if (property.ContainingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (property.IsReadOnly)
            {
                return true;
            }

            if (property.GetMethod is null)
            {
                return property.SetMethod.IsReadOnly;
            }
            
            if (property.SetMethod is null)
            {
                return property.GetMethod.IsReadOnly;
            }
            
            return property.GetMethod.IsReadOnly && property.SetMethod.IsReadOnly;
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
            if (symbol.IsVirtual)
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (IsPropertyReadonly(symbol))
            {
                yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
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

        private static IEnumerable<AccessorDeclarationSyntax> GetPropertyAccessors(IPropertySymbol propertySymbol, IFilterVisitor filterVisitor)
        {
            var isPropertyReadonly = IsPropertyReadonly(propertySymbol);

            var getAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.GetMethod, SyntaxKind.GetAccessorDeclaration, SyntaxKind.GetKeyword, filterVisitor, isPropertyReadonly);
            if (getAccessor != null)
            {
                yield return getAccessor;
            }

            var setAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.SetMethod, SyntaxKind.SetAccessorDeclaration, SyntaxKind.SetKeyword, filterVisitor, isPropertyReadonly);
            if (setAccessor != null)
            {
                yield return setAccessor;
            }
        }

        private static AccessorDeclarationSyntax GetPropertyAccessorCore(
            IPropertySymbol propertySymbol, IMethodSymbol methodSymbol,
            SyntaxKind kind, SyntaxKind keyword, IFilterVisitor filterVisitor, bool isPropertyReadonly)
        {
            if (methodSymbol == null)
            {
                return null;
            }

            var modifiers = new SyntaxTokenList();

            if (methodSymbol.IsReadOnly && !isPropertyReadonly)
            {
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            }

            switch (methodSymbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    if (propertySymbol.DeclaredAccessibility == Accessibility.Protected ||
                        propertySymbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            modifiers,
                            SyntaxFactory.Token(keyword),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    else
                    {
                        return SyntaxFactory.AccessorDeclaration(
                            kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            modifiers.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
                            SyntaxFactory.Token(keyword),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                case Accessibility.Public:
                    return SyntaxFactory.AccessorDeclaration(kind,
                        GetAttributes(methodSymbol, filterVisitor),
                        modifiers,
                        SyntaxFactory.Token(keyword),
                        (BlockSyntax)null,
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                default:
                    if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            modifiers,
                            SyntaxFactory.Token(keyword),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    return null;
            }
        }

        private static string RemoveBraces(string text)
        {
            return BracesRegex.Replace(text, string.Empty);
        }

        private static TypeSyntax GetRefType(TypeSyntax typeSyntax, RefKind refKind)
        {
            if (refKind == RefKind.Ref)
            {
                typeSyntax = SyntaxFactory.RefType(
                    SyntaxFactory.Token(SyntaxKind.RefKeyword),
                    typeSyntax
                );
            }
            else if (refKind == RefKind.RefReadOnly)
            {
                typeSyntax = SyntaxFactory.RefType(
                    SyntaxFactory.Token(SyntaxKind.RefKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword),
                    typeSyntax
                );
            }

            return typeSyntax;
        }

        private static TypeSyntax GetMethodTypeSyntax(IMethodSymbol method)
        {
            var typeSyntax = GetTypeSyntax(method.ReturnType);
            return GetRefType(typeSyntax, method.RefKind);
        }

        private static TypeSyntax GetPropertyTypeSyntax(IPropertySymbol property)
        {
            var typeSyntax = GetTypeSyntax(property.Type);
            return GetRefType(typeSyntax, property.RefKind);
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
