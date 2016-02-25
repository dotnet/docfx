// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;

    using Microsoft.DocAsCode.Utility;

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
                case TypeKind.Class:
                    modifiers.Add("Class");
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
            string syntaxStr = null;
            switch (typeKind)
            {
                case MemberType.Class:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.ClassBlock(
                            SyntaxFactory.ClassStatement(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                                SyntaxFactory.Identifier(typeSymbol.Name),
                                GetTypeParameters(typeSymbol)),
                            GetInheritsList(typeSymbol),
                            GetImplementsList(typeSymbol),
                            new SyntaxList<StatementSyntax>(),
                            SyntaxFactory.EndClassStatement())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Enum:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.EnumBlock(
                            SyntaxFactory.EnumStatement(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                                SyntaxFactory.Token(SyntaxKind.EnumKeyword),
                                SyntaxFactory.Identifier(typeSymbol.Name),
                                GetEnumUnderlyingType(typeSymbol)))
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Interface:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.InterfaceBlock(
                            SyntaxFactory.InterfaceStatement(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                                SyntaxFactory.Token(SyntaxKind.InterfaceKeyword),
                                SyntaxFactory.Identifier(typeSymbol.Name),
                                GetTypeParameters(typeSymbol)),
                            GetInheritsList(typeSymbol),
                            new SyntaxList<ImplementsStatementSyntax>(),
                            new SyntaxList<StatementSyntax>(),
                            SyntaxFactory.EndInterfaceStatement())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Struct:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.StructureBlock(
                            SyntaxFactory.StructureStatement(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                                SyntaxFactory.Token(SyntaxKind.StructureKeyword),
                                SyntaxFactory.Identifier(typeSymbol.Name),
                                GetTypeParameters(typeSymbol)),
                            new SyntaxList<InheritsStatementSyntax>(),
                            GetImplementsList(typeSymbol),
                            new SyntaxList<StatementSyntax>(),
                            SyntaxFactory.EndStructureStatement())
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Delegate:
                    {
                        var typeSymbol = (INamedTypeSymbol)symbol;
                        syntaxStr = SyntaxFactory.DelegateStatement(
                            typeSymbol.DelegateInvokeMethod.ReturnsVoid ? SyntaxKind.DelegateSubStatement : SyntaxKind.DelegateFunctionStatement,
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetTypeModifiers(typeSymbol)),
                            typeSymbol.DelegateInvokeMethod.ReturnsVoid ? SyntaxFactory.Token(SyntaxKind.SubKeyword) : SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                            SyntaxFactory.Identifier(typeSymbol.Name),
                            GetTypeParameters(typeSymbol),
                            GetParamerterList(typeSymbol.DelegateInvokeMethod),
                            GetReturnAsClause(typeSymbol.DelegateInvokeMethod))
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Method:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        syntaxStr = SyntaxFactory.MethodStatement(
                            methodSymbol.ReturnsVoid ? SyntaxKind.SubStatement : SyntaxKind.FunctionStatement,
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                            methodSymbol.ReturnsVoid ? SyntaxFactory.Token(SyntaxKind.SubKeyword) : SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                            SyntaxFactory.Identifier(methodSymbol.Name),
                            GetTypeParameters(methodSymbol),
                            GetParamerterList(methodSymbol),
                            GetReturnAsClause(methodSymbol),
                            null,
                            GetImplementsClause(methodSymbol))
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Operator:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        var operatorToken = GetOperatorToken(methodSymbol);
                        if (operatorToken == null)
                        {
                            syntaxStr = "VB cannot support this operator.";
                        }
                        else
                        {
                            syntaxStr = SyntaxFactory.OperatorStatement(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                                SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                                operatorToken.Value,
                                GetParamerterList(methodSymbol),
                                GetReturnAsClause(methodSymbol))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        break;
                    };
                case MemberType.Constructor:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;
                        syntaxStr = SyntaxFactory.SubNewStatement(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(methodSymbol)),
                            GetParamerterList(methodSymbol))
                            .NormalizeWhitespace()
                            .ToString();
                        syntaxStr = RemoveEnd(syntaxStr);
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
                                GetDefaultValue(fieldSymbol))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        else
                        {
                            syntaxStr = SyntaxFactory.FieldDeclaration(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(GetMemberModifiers(fieldSymbol)),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.ModifiedIdentifier(symbol.Name)),
                                        fieldSymbol.ContainingType.TypeKind == TypeKind.Enum ?
                                            null :
                                            SyntaxFactory.SimpleAsClause(
                                                GetTypeSyntax(fieldSymbol.Type)),
                                        GetDefaultValue(fieldSymbol))))
                                .NormalizeWhitespace()
                                .ToString();
                        }
                        syntaxStr = RemoveEnd(syntaxStr);
                        break;
                    };
                case MemberType.Event:
                    {
                        var eventSymbol = (IEventSymbol)symbol;
                        syntaxStr = SyntaxFactory.EventStatement(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(eventSymbol)),
                            SyntaxFactory.Identifier(eventSymbol.Name),
                            null,
                            SyntaxFactory.SimpleAsClause(
                                GetTypeSyntax(eventSymbol.Type)),
                            GetImplementsClause(eventSymbol))
                            .NormalizeWhitespace()
                            .ToString();
                        break;
                    };
                case MemberType.Property:
                    {
                        var propertySymbol = (IPropertySymbol)symbol;
                        syntaxStr = SyntaxFactory.PropertyStatement(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(GetMemberModifiers(propertySymbol)),
                            SyntaxFactory.Identifier(propertySymbol.MetadataName),
                            GetParamerterList(propertySymbol),
                            SyntaxFactory.SimpleAsClause(
                                GetTypeSyntax(propertySymbol.Type)),
                            null,
                            GetImplementsClause(propertySymbol))
                            .NormalizeWhitespace()
                            .ToString();
                        break;
                    };
            }

            return syntaxStr;
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter)
        {
            symbol.Accept(new VBReferenceItemVisitor(reference));
        }

        #endregion

        #region Private Methods

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

        private ImplementsClauseSyntax GetImplementsClause(IMethodSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where VisitorHelper.CanVisit(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), (SimpleNameSyntax)SyntaxFactory.ParseName(eii.Name))).ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.ImplementsClause(SyntaxFactory.SeparatedList(list.ToArray()));
        }

        private ImplementsClauseSyntax GetImplementsClause(IEventSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where VisitorHelper.CanVisit(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), (SimpleNameSyntax)SyntaxFactory.ParseName(eii.Name))).ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.ImplementsClause(SyntaxFactory.SeparatedList(list.ToArray()));
        }

        private ImplementsClauseSyntax GetImplementsClause(IPropertySymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            var list = (from eii in symbol.ExplicitInterfaceImplementations
                        where VisitorHelper.CanVisit(eii)
                        select SyntaxFactory.QualifiedName(GetQualifiedNameSyntax(eii.ContainingType), (SimpleNameSyntax)SyntaxFactory.ParseName(eii.Name))).ToList();
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
                return GetDefaultValueCore(symbol.ExplicitDefaultValue);
            }
            return null;
        }

        private EqualsValueSyntax GetDefaultValue(IFieldSymbol symbol)
        {
            if (symbol.IsConst)
            {
                return GetDefaultValueCore(symbol.ConstantValue);
            }
            return null;
        }

        private EqualsValueSyntax GetDefaultValueCore(object value)
        {
            if (value == null)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NothingLiteralExpression,
                        SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
            }
            if (value is bool)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        (bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression,
                        SyntaxFactory.Token(
                            (bool)value ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword)));
            }
            if (value is long)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((long)value)));
            }
            if (value is ulong)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ulong)value)));
            }
            if (value is int)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((int)value)));
            }
            if (value is uint)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((uint)value)));
            }
            if (value is short)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((short)value)));
            }
            if (value is ushort)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ushort)value)));
            }
            if (value is byte)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((byte)value)));
            }
            if (value is sbyte)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((sbyte)value)));
            }
            if (value is double)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((double)value)));
            }
            if (value is float)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((float)value)));
            }
            if (value is decimal)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((decimal)value)));
            }
            if (value is char)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.CharacterLiteralExpression,
                        SyntaxFactory.Literal((char)value)));
            }
            if (value is string)
            {
                return SyntaxFactory.EqualsValue(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal((string)value)));
            }

            Debug.Fail("Unknown default value!");
            return null;
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
