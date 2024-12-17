// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

#nullable enable

namespace Docfx.Dotnet;

partial class SymbolFormatter
{
    class SyntaxFormatter
    {
        private static readonly SymbolDisplayFormat s_syntaxFormat = new(
            kindOptions:
                SymbolDisplayKindOptions.IncludeNamespaceKeyword |
                SymbolDisplayKindOptions.IncludeMemberKeyword |
                SymbolDisplayKindOptions.IncludeTypeKeyword,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeConstantValue |
                SymbolDisplayMemberOptions.IncludeModifiers |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface,
            genericsOptions:
                SymbolDisplayGenericsOptions.IncludeTypeParameters |
                SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                SymbolDisplayGenericsOptions.IncludeVariance,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeExtensionThis,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix |
                (ExtractMetadataConfig.UseClrTypeNames
                    ? SymbolDisplayMiscellaneousOptions.None
                    : SymbolDisplayMiscellaneousOptions.UseSpecialTypes),
            localOptions: SymbolDisplayLocalOptions.IncludeType,
            propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
            delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        private static readonly SymbolDisplayFormat s_syntaxTypeNameFormat = new(
            genericsOptions:
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                (ExtractMetadataConfig.UseClrTypeNames
                    ? SymbolDisplayMiscellaneousOptions.None
                    : SymbolDisplayMiscellaneousOptions.UseSpecialTypes),
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        private static readonly SymbolDisplayFormat s_syntaxEnumConstantFormat = s_syntaxFormat
            .WithMemberOptions(s_syntaxFormat.MemberOptions | SymbolDisplayMemberOptions.IncludeContainingType);

        public SyntaxLanguage Language { get; init; }
        public SymbolFilter Filter { get; init; } = default!;

        private readonly ImmutableArray<SymbolDisplayPart>.Builder _parts = ImmutableArray.CreateBuilder<SymbolDisplayPart>();

        public ImmutableArray<SymbolDisplayPart> GetSyntax(ISymbol symbol)
        {
            AddAttributes(symbol);
            AddAccessibility(symbol);
            AddModifiersIfNeeded(symbol);

            symbol = HidePropertyAccessorIfNeeded(symbol);

            foreach (var part in GetDisplayParts(symbol, s_syntaxFormat))
            {
                if (ExpandEnumClassName(symbol, part))
                    continue;

                if (StaticClassToVBModule(symbol, part))
                    continue;

                _parts.Add(part);
            }

            var namedTypeConstraints = RemoveNamedTypeConstraints(symbol);
            AddBaseTypeAndInterfaces(symbol);
            _parts.AddRange(namedTypeConstraints);

            return _parts.ToImmutable();
        }

        private void AddAccessibility(ISymbol symbol)
        {
            if (symbol.IsEnumMember() || symbol.IsInstanceInterfaceMember() || symbol.IsExplicitInterfaceImplementation())
                return;

            switch (Filter.GetDisplayAccessibility(symbol))
            {
                case Accessibility.Public:
                    AddKeyword("public", "Public");
                    AddSpace();
                    break;

                case Accessibility.Protected:
                    AddKeyword("protected", "Protected");
                    AddSpace();
                    break;

                case Accessibility.ProtectedOrInternal:
                    AddKeyword("protected", "Protected");
                    AddSpace();
                    AddKeyword("internal", "Friend");
                    AddSpace();
                    break;

                case Accessibility.ProtectedAndInternal:
                    AddKeyword("private", "Private");
                    AddSpace();
                    AddKeyword("protected", "Protected");
                    AddSpace();
                    break;

                case Accessibility.Internal:
                    AddKeyword("internal", "Friend");
                    AddSpace();
                    break;

                case Accessibility.Private:
                    AddKeyword("private", "Private");
                    AddSpace();
                    break;
            }
        }

        private void AddModifiersIfNeeded(ISymbol symbol)
        {
            if (symbol.IsClass())
            {
                if (symbol.IsStatic && Language is not SyntaxLanguage.VB)
                {
                    AddKeyword("static");
                    AddSpace();
                }

                if (symbol.IsAbstract)
                {
                    AddKeyword("abstract", "MustInherit");
                    AddSpace();
                }

                if (symbol.IsSealed)
                {
                    AddKeyword("sealed", "NotInheritable");
                    AddSpace();
                }
            }

            if (symbol.IsStaticInterfaceMember())
            {
                var isConst = symbol is IFieldSymbol { IsConst: true };
                if (symbol.IsStatic && !isConst)
                {
                    AddKeyword("static", "Shared");
                    AddSpace();
                }

                if (symbol.IsAbstract)
                {
                    AddKeyword("abstract", "MustInherit");
                    AddSpace();
                }
            }
        }

        private ImmutableArray<SymbolDisplayPart> RemoveNamedTypeConstraints(ISymbol symbol)
        {
            if (symbol.Kind is not SymbolKind.NamedType)
                return [];

            var result = ImmutableArray.CreateBuilder<SymbolDisplayPart>();

            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Kind == SymbolDisplayPartKind.Keyword && part.ToString() == "where")
                {
                    result.Add(new(SymbolDisplayPartKind.Space, null, " "));
                    while (i < _parts.Count)
                    {
                        result.Add(_parts[i]);
                        _parts.RemoveAt(i);
                    }
                    RemoveEnd();
                    break;
                }
            }

            return result.ToImmutable();
        }

        private void AddBaseTypeAndInterfaces(ISymbol symbol)
        {
            if (symbol.Kind is not SymbolKind.NamedType || symbol is not INamedTypeSymbol type)
                return;

            var baseTypes = new List<INamedTypeSymbol>();

            if (type.TypeKind is TypeKind.Enum)
            {
                if (type.EnumUnderlyingType is not null && type.EnumUnderlyingType.SpecialType is not SpecialType.System_Int32)
                {
                    if (Language is SyntaxLanguage.VB)
                    {
                        AddSpace();
                        AddKeyword("As");
                        AddSpace();
                        AddTypeName(type.EnumUnderlyingType);
                    }
                    else
                    {
                        baseTypes.Add(type.EnumUnderlyingType);
                    }
                }
            }
            else if (type.TypeKind is TypeKind.Class or TypeKind.Interface or TypeKind.Struct)
            {
                if (type.BaseType is not null &&
                    type.BaseType.SpecialType is not SpecialType.System_Object &&
                    type.BaseType.SpecialType is not SpecialType.System_ValueType)
                {
                    if (Language is SyntaxLanguage.VB)
                    {
                        AddSpace();
                        AddKeyword("Inherits");
                        AddSpace();
                        AddTypeName(type.BaseType);
                    }
                    else
                    {
                        baseTypes.Add(type.BaseType);
                    }
                }

                foreach (var @interface in type.AllInterfaces)
                {
                    if (Filter.IncludeApi(@interface))
                        baseTypes.Add(@interface);
                }
            }

            if (baseTypes.Count <= 0)
                return;

            AddSpace();
            if (Language is SyntaxLanguage.VB)
                AddKeyword(type.TypeKind is TypeKind.Interface ? "Inherits" : "Implements");
            else
                AddPunctuation(":");
            AddSpace();

            foreach (var baseType in baseTypes)
            {
                AddTypeName(baseType);
                AddPunctuation(",");
                AddSpace();
            }

            RemoveEnd();
            RemoveEnd();
        }

        private void AddTypeName(ITypeSymbol symbol)
        {
            _parts.AddRange(GetDisplayParts(symbol, s_syntaxTypeNameFormat));
        }

        private void AddAttributes(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeConstructor is null ||
                    attribute.AttributeClass is null ||
                    !Filter.IncludeAttribute(attribute.AttributeConstructor))
                {
                    continue;
                }

                AddKeyword("[", "<");

                foreach (var part in GetDisplayParts(attribute.AttributeClass, s_syntaxTypeNameFormat))
                {
                    _parts.Add(
                        part.Kind == SymbolDisplayPartKind.ClassName && part.ToString().EndsWith("Attribute")
                            ? new(part.Kind, part.Symbol, part.ToString()[..^9])
                            : part);
                }

                AddAttributeArguments(attribute);
                AddKeyword("]", ">");
                AddLineBreak();
            }
        }

        private void AddAttributeArguments(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0 && attribute.NamedArguments.Length == 0)
                return;

            AddKeyword("(");

            foreach (var argument in attribute.ConstructorArguments)
            {
                AddTypedConstant(argument);
                AddPunctuation(",");
                AddSpace();
            }

            foreach (var (key, argument) in attribute.NamedArguments)
            {
                _parts.Add(new(SymbolDisplayPartKind.ParameterName, null, key));

                if (Language is SyntaxLanguage.VB)
                {
                    AddPunctuation(":=");
                }
                else
                {
                    AddSpace();
                    AddPunctuation(Language is SyntaxLanguage.VB ? ":=" : "=");
                    AddSpace();
                }

                AddTypedConstant(argument);
                AddPunctuation(",");
                AddSpace();
            }

            RemoveEnd();
            RemoveEnd();
            AddKeyword(")");
        }

        private void AddTypedConstant(TypedConstant typedConstant)
        {
            switch (typedConstant.Kind)
            {
                case TypedConstantKind.Primitive when typedConstant.Value is not null:
                    var value = Language is SyntaxLanguage.VB
                        ? VB.SymbolDisplay.FormatPrimitive(typedConstant.Value, quoteStrings: true, useHexadecimalNumbers: false)
                        : CS.SymbolDisplay.FormatPrimitive(typedConstant.Value, quoteStrings: true, useHexadecimalNumbers: false);
                    _parts.Add(new(typedConstant.Value is string ? SymbolDisplayPartKind.StringLiteral : SymbolDisplayPartKind.NumericLiteral, null, value));
                    break;

                case TypedConstantKind.Enum:
                    AddEnumConstant(typedConstant);
                    break;

                case TypedConstantKind.Type when typedConstant.Value is ITypeSymbol typeSymbol:
                    AddKeyword("typeof", "GetType");
                    AddPunctuation("(");
                    AddTypeName(typeSymbol);
                    AddPunctuation(")");
                    break;

                case TypedConstantKind.Array when typedConstant is { IsNull: false, Type: not null }:
                    AddKeyword("new", "New");
                    AddSpace();
                    AddTypeName(typedConstant.Type);
                    AddSpace();
                    AddPunctuation("{");
                    AddSpace();

                    if (typedConstant.Values.Length > 0)
                    {
                        foreach (var item in typedConstant.Values)
                        {
                            AddTypedConstant(item);
                            AddPunctuation(",");
                            AddSpace();
                        }

                        RemoveEnd();
                        RemoveEnd();
                        AddSpace();
                    }

                    AddPunctuation("}");
                    break;

                default:
                    AddKeyword("null", "Nothing");
                    break;
            }
        }

        private void RemoveEnd()
        {
            _parts.RemoveAt(_parts.Count - 1);
        }

        private void AddKeyword(string csharp, string vb)
        {
            _parts.Add(new(SymbolDisplayPartKind.Keyword, null, Language is SyntaxLanguage.VB ? vb : csharp));
        }

        private void AddKeyword(string text)
        {
            _parts.Add(new(SymbolDisplayPartKind.Keyword, null, text));
        }

        private void AddSpace()
        {
            _parts.Add(new(SymbolDisplayPartKind.Space, null, " "));
        }

        private void AddLineBreak()
        {
            _parts.Add(new(SymbolDisplayPartKind.LineBreak, null, "\r\n"));
        }

        private void AddPunctuation(string text)
        {
            _parts.Add(new(SymbolDisplayPartKind.Punctuation, null, text));
        }

        private void AddEnumConstant(TypedConstant typedConstant)
        {
            var parameterSymbol = new ParameterSymbol
            {
                Type = typedConstant.Type,
                HasExplicitDefaultValue = true,
                ExplicitDefaultValue = typedConstant.Value,
            };

            var add = false;
            var parts = GetDisplayParts(parameterSymbol, s_syntaxEnumConstantFormat);

            foreach (var part in parts)
            {
                if (add)
                {
                    if (part.Kind != SymbolDisplayPartKind.Space)
                    {
                        _parts.Add(part);
                    }
                }
                else if (part.Kind == SymbolDisplayPartKind.Punctuation && part.ToString() == "=")
                {
                    add = true;
                    continue;
                }
            }
        }

        private bool ExpandEnumClassName(ISymbol symbol, SymbolDisplayPart part)
        {
            if (symbol.Kind != SymbolKind.Field && part is { Kind: SymbolDisplayPartKind.EnumMemberName, Symbol: not null })
            {
                _parts.Add(new(SymbolDisplayPartKind.EnumName, part.Symbol.ContainingSymbol, part.Symbol.ContainingSymbol.Name));
                _parts.Add(new(SymbolDisplayPartKind.Punctuation, null, "."));
                _parts.Add(part);
                return true;
            }
            return false;
        }

        private bool StaticClassToVBModule(ISymbol symbol, SymbolDisplayPart part)
        {
            if (Language is SyntaxLanguage.VB && symbol is { IsStatic: true, Kind: SymbolKind.NamedType } &&
                part.Kind == SymbolDisplayPartKind.Keyword && part.ToString() == "Class")
            {
                _parts.Add(new(SymbolDisplayPartKind.Keyword, null, "Module"));
                return true;
            }
            return false;
        }

        private ISymbol HidePropertyAccessorIfNeeded(ISymbol symbol)
        {
            if (symbol is not IPropertySymbol property)
                return symbol;

            var accessibility = Filter.GetDisplayAccessibility(property);
            if (accessibility is null)
                return symbol;

            return new PropertySymbol
            {
                Inner = property,
                DeclaredAccessibility = accessibility.Value,
                GetMethod = GetAccessor(property.GetMethod),
                SetMethod = GetAccessor(property.SetMethod),
            };

            IMethodSymbol? GetAccessor(IMethodSymbol? method)
            {
                if (method is null)
                    return null;

                var accessibility = Filter.GetDisplayAccessibility(method);
                if (accessibility is null)
                    return null;

                if (accessibility == method.DeclaredAccessibility)
                    return method;

                return new MethodSymbol { Inner = method, DeclaredAccessibility = accessibility.Value };
            }
        }

        private ImmutableArray<SymbolDisplayPart> GetDisplayParts(ISymbol symbol, SymbolDisplayFormat format)
        {
            try
            {
                return Language is SyntaxLanguage.VB
                    ? VB.SymbolDisplay.ToDisplayParts(symbol, format)
                    : CS.SymbolDisplay.ToDisplayParts(symbol, format);
            }
            catch
            {
                return [];
            }
        }
    }
}
