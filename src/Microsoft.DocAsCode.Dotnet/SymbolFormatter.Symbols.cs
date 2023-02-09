using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1009

namespace Microsoft.DocAsCode.Dotnet
{
    partial class SymbolFormatter
    {
        private class ParameterSymbol : IParameterSymbol
        {
            public RefKind RefKind => default;

            public ScopedKind ScopedKind => default;

            public bool IsParams => false;

            public bool IsOptional => false;

            public bool IsThis => false;

            public bool IsDiscard => false;

            public ITypeSymbol Type { get; init; }

            public NullableAnnotation NullableAnnotation => default;

            public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public int Ordinal => 0;

            public bool HasExplicitDefaultValue { get; init; }

            public object ExplicitDefaultValue { get; init; }

            public IParameterSymbol OriginalDefinition => throw new NotImplementedException();

            public SymbolKind Kind => SymbolKind.Parameter;

            public string Language => throw new NotImplementedException();

            public string Name => "";

            public string MetadataName => throw new NotImplementedException();

            public int MetadataToken => throw new NotImplementedException();

            public ISymbol ContainingSymbol => throw new NotImplementedException();

            public IAssemblySymbol ContainingAssembly => throw new NotImplementedException();

            public IModuleSymbol ContainingModule => throw new NotImplementedException();

            public INamedTypeSymbol ContainingType => throw new NotImplementedException();

            public INamespaceSymbol ContainingNamespace => throw new NotImplementedException();

            public bool IsDefinition => false;

            public bool IsStatic => false;

            public bool IsVirtual => false;

            public bool IsOverride => false;

            public bool IsAbstract => false;

            public bool IsSealed => false;

            public bool IsExtern => false;

            public bool IsImplicitlyDeclared => false;

            public bool CanBeReferencedByName => false;

            public ImmutableArray<Location> Locations => throw new NotImplementedException();

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

            public Accessibility DeclaredAccessibility => throw new NotImplementedException();

            public bool HasUnsupportedMetadata => false;

            ISymbol ISymbol.OriginalDefinition => throw new NotImplementedException();

            public void Accept(SymbolVisitor visitor) => visitor.VisitParameter(this);
            public TResult? Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotImplementedException();
            public TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument) => throw new NotImplementedException();
            public bool Equals([NotNullWhen(true)] ISymbol other, SymbolEqualityComparer equalityComparer) => throw new NotImplementedException();
            public bool Equals(ISymbol other) => throw new NotImplementedException();
            public ImmutableArray<AttributeData> GetAttributes() => throw new NotImplementedException();
            public string GetDocumentationCommentId() => throw new NotImplementedException();
            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public string ToDisplayString(SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();
        }

        private class PropertySymbol : IPropertySymbol
        {
            public IPropertySymbol Inner { get; init; }

            public bool IsIndexer => Inner.IsIndexer;

            public bool IsReadOnly => Inner.IsReadOnly;

            public bool IsWriteOnly => Inner.IsWriteOnly;

            public bool IsRequired => Inner.IsRequired;

            public bool IsWithEvents => Inner.IsWithEvents;

            public bool ReturnsByRef => Inner.ReturnsByRef;

            public bool ReturnsByRefReadonly => Inner.ReturnsByRefReadonly;

            public RefKind RefKind => Inner.RefKind;

            public ITypeSymbol Type => Inner.Type;

            public NullableAnnotation NullableAnnotation => Inner.NullableAnnotation;

            public ImmutableArray<IParameterSymbol> Parameters => Inner.Parameters;

            public IMethodSymbol GetMethod { get; init; }

            public IMethodSymbol SetMethod { get; init; }

            public IPropertySymbol OriginalDefinition => Inner.OriginalDefinition;

            public IPropertySymbol OverriddenProperty => Inner.OverriddenProperty;

            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations => Inner.ExplicitInterfaceImplementations;

            public ImmutableArray<CustomModifier> RefCustomModifiers => Inner.RefCustomModifiers;

            public ImmutableArray<CustomModifier> TypeCustomModifiers => Inner.TypeCustomModifiers;

            public SymbolKind Kind => Inner.Kind;

            public string Language => Inner.Language;

            public string Name => Inner.Name;

            public string MetadataName => Inner.MetadataName;

            public int MetadataToken => Inner.MetadataToken;

            public ISymbol ContainingSymbol => Inner.ContainingSymbol;

            public IAssemblySymbol ContainingAssembly => Inner.ContainingAssembly;

            public IModuleSymbol ContainingModule => Inner.ContainingModule;

            public INamedTypeSymbol ContainingType => Inner.ContainingType;

            public INamespaceSymbol ContainingNamespace => Inner.ContainingNamespace;

            public bool IsDefinition => Inner.IsDefinition;

            public bool IsStatic => Inner.IsStatic;

            public bool IsVirtual => Inner.IsVirtual;

            public bool IsOverride => Inner.IsOverride;

            public bool IsAbstract => Inner.IsAbstract;

            public bool IsSealed => Inner.IsSealed;

            public bool IsExtern => Inner.IsExtern;

            public bool IsImplicitlyDeclared => Inner.IsImplicitlyDeclared;

            public bool CanBeReferencedByName => Inner.CanBeReferencedByName;

            public ImmutableArray<Location> Locations => Inner.Locations;

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => Inner.DeclaringSyntaxReferences;

            public Accessibility DeclaredAccessibility { get; init; }

            public bool HasUnsupportedMetadata => Inner.HasUnsupportedMetadata;

            ISymbol ISymbol.OriginalDefinition => ((ISymbol)Inner).OriginalDefinition;

            public void Accept(SymbolVisitor visitor) => visitor.VisitProperty(this);
            public TResult? Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitProperty(this);
            public TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitProperty(this, argument);
            public bool Equals([NotNullWhen(true)] ISymbol other, SymbolEqualityComparer equalityComparer) => Inner.Equals(other, equalityComparer);
            public bool Equals(ISymbol other) => Inner.Equals(other);
            public ImmutableArray<AttributeData> GetAttributes() => Inner.GetAttributes();
            public string GetDocumentationCommentId() => Inner.GetDocumentationCommentId();
            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => Inner.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null) => Inner.ToDisplayParts(format);
            public string ToDisplayString(SymbolDisplayFormat format = null) => Inner.ToDisplayString(format);
            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => Inner.ToMinimalDisplayParts(semanticModel, position, format);
            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => Inner.ToMinimalDisplayString(semanticModel, position, format);
        }

        public class MethodSymbol : IMethodSymbol
        {
            public IMethodSymbol Inner { get; init; }

            public MethodKind MethodKind => Inner.MethodKind;

            public int Arity => Inner.Arity;

            public bool IsGenericMethod => Inner.IsGenericMethod;

            public bool IsExtensionMethod => Inner.IsExtensionMethod;

            public bool IsAsync => Inner.IsAsync;

            public bool IsVararg => Inner.IsVararg;

            public bool IsCheckedBuiltin => Inner.IsCheckedBuiltin;

            public bool HidesBaseMethodsByName => Inner.HidesBaseMethodsByName;

            public bool ReturnsVoid => Inner.ReturnsVoid;

            public bool ReturnsByRef => Inner.ReturnsByRef;

            public bool ReturnsByRefReadonly => Inner.ReturnsByRefReadonly;

            public RefKind RefKind => Inner.RefKind;

            public ITypeSymbol ReturnType => Inner.ReturnType;

            public NullableAnnotation ReturnNullableAnnotation => Inner.ReturnNullableAnnotation;

            public ImmutableArray<ITypeSymbol> TypeArguments => Inner.TypeArguments;

            public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => Inner.TypeArgumentNullableAnnotations;

            public ImmutableArray<ITypeParameterSymbol> TypeParameters => Inner.TypeParameters;

            public ImmutableArray<IParameterSymbol> Parameters => Inner.Parameters;

            public IMethodSymbol ConstructedFrom => Inner.ConstructedFrom;

            public bool IsReadOnly => Inner.IsReadOnly;

            public bool IsInitOnly => Inner.IsInitOnly;

            public IMethodSymbol OriginalDefinition => Inner.OriginalDefinition;

            public IMethodSymbol OverriddenMethod => Inner.OverriddenMethod;

            public ITypeSymbol ReceiverType => Inner.ReceiverType;

            public NullableAnnotation ReceiverNullableAnnotation => Inner.ReceiverNullableAnnotation;

            public IMethodSymbol ReducedFrom => Inner.ReducedFrom;

            public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations => Inner.ExplicitInterfaceImplementations;

            public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => Inner.ReturnTypeCustomModifiers;

            public ImmutableArray<CustomModifier> RefCustomModifiers => Inner.RefCustomModifiers;

            public SignatureCallingConvention CallingConvention => Inner.CallingConvention;

            public ImmutableArray<INamedTypeSymbol> UnmanagedCallingConventionTypes => Inner.UnmanagedCallingConventionTypes;

            public ISymbol AssociatedSymbol => Inner.AssociatedSymbol;

            public IMethodSymbol PartialDefinitionPart => Inner.PartialDefinitionPart;

            public IMethodSymbol PartialImplementationPart => Inner.PartialImplementationPart;

            public MethodImplAttributes MethodImplementationFlags => Inner.MethodImplementationFlags;

            public bool IsPartialDefinition => Inner.IsPartialDefinition;

            public INamedTypeSymbol AssociatedAnonymousDelegate => Inner.AssociatedAnonymousDelegate;

            public bool IsConditional => Inner.IsConditional;

            public SymbolKind Kind => Inner.Kind;

            public string Language => Inner.Language;

            public string Name => Inner.Name;

            public string MetadataName => Inner.MetadataName;

            public int MetadataToken => Inner.MetadataToken;

            public ISymbol ContainingSymbol => Inner.ContainingSymbol;

            public IAssemblySymbol ContainingAssembly => Inner.ContainingAssembly;

            public IModuleSymbol ContainingModule => Inner.ContainingModule;

            public INamedTypeSymbol ContainingType => Inner.ContainingType;

            public INamespaceSymbol ContainingNamespace => Inner.ContainingNamespace;

            public bool IsDefinition => Inner.IsDefinition;

            public bool IsStatic => Inner.IsStatic;

            public bool IsVirtual => Inner.IsVirtual;

            public bool IsOverride => Inner.IsOverride;

            public bool IsAbstract => Inner.IsAbstract;

            public bool IsSealed => Inner.IsSealed;

            public bool IsExtern => Inner.IsExtern;

            public bool IsImplicitlyDeclared => Inner.IsImplicitlyDeclared;

            public bool CanBeReferencedByName => Inner.CanBeReferencedByName;

            public ImmutableArray<Location> Locations => Inner.Locations;

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => Inner.DeclaringSyntaxReferences;

            public Accessibility DeclaredAccessibility { get; init; }

            public bool HasUnsupportedMetadata => Inner.HasUnsupportedMetadata;

            ISymbol ISymbol.OriginalDefinition => ((ISymbol)Inner).OriginalDefinition;

            public void Accept(SymbolVisitor visitor) => Inner.Accept(visitor);
            public TResult? Accept<TResult>(SymbolVisitor<TResult> visitor) => Inner.Accept(visitor);
            public TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument) => Inner.Accept(visitor, argument);
            public IMethodSymbol Construct(params ITypeSymbol[] typeArguments) => Inner.Construct(typeArguments);
            public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations) => Inner.Construct(typeArguments, typeArgumentNullableAnnotations);
            public bool Equals([NotNullWhen(true)] ISymbol other, SymbolEqualityComparer equalityComparer) => Inner.Equals(other, equalityComparer);
            public bool Equals(ISymbol other) => Inner.Equals(other);
            public ImmutableArray<AttributeData> GetAttributes() => Inner.GetAttributes();
            public DllImportData GetDllImportData() => Inner.GetDllImportData();
            public string GetDocumentationCommentId() => Inner.GetDocumentationCommentId();
            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => Inner.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            public ImmutableArray<AttributeData> GetReturnTypeAttributes() => Inner.GetReturnTypeAttributes();
            public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter) => Inner.GetTypeInferredDuringReduction(reducedFromTypeParameter);
            public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType) => Inner.ReduceExtensionMethod(receiverType);
            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null) => Inner.ToDisplayParts(format);
            public string ToDisplayString(SymbolDisplayFormat format = null) => Inner.ToDisplayString(format);
            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => Inner.ToMinimalDisplayParts(semanticModel, position, format);
            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => Inner.ToMinimalDisplayString(semanticModel, position, format);
        }
    }
}
