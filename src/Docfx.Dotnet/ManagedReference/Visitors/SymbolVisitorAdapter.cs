// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet;

internal partial class SymbolVisitorAdapter : SymbolVisitor<MetadataItem>
{
    [GeneratedRegex(@"^([\w\{\}`]+\.)+")]
    private static partial Regex MemberSigRegex();

    private static readonly IReadOnlyList<string> EmptyListOfString = Array.Empty<string>();
    private readonly Compilation _compilation;
    private readonly YamlModelGenerator _generator;
    private readonly Dictionary<string, ReferenceItem> _references = [];
    private readonly IMethodSymbol[] _extensionMethods;
    private readonly ExtractMetadataConfig _config;
    private readonly SymbolFilter _filter;

    public SymbolVisitorAdapter(Compilation compilation, YamlModelGenerator generator, ExtractMetadataConfig config, SymbolFilter filter, IMethodSymbol[] extensionMethods)
    {
        _compilation = compilation;
        _generator = generator;
        _filter = filter;
        _config = config;
        _extensionMethods = extensionMethods?.Where(_filter.IncludeApi).ToArray() ?? [];
    }

    public override MetadataItem DefaultVisit(ISymbol symbol)
    {
        if (!_filter.IncludeApi(symbol))
        {
            return null;
        }

        var item = new MetadataItem
        {
            Name = VisitorHelper.GetId(symbol),
            CommentId = VisitorHelper.GetCommentId(symbol),
            DisplayNames = [],
            DisplayNamesWithType = [],
            DisplayQualifiedNames = [],
            Source = _config.DisableGitFeatures ? null : VisitorHelper.GetSourceDetail(symbol, _compilation),
        };
        var assemblyName = symbol.ContainingAssembly?.Name;
        item.AssemblyNameList = string.IsNullOrEmpty(assemblyName) || assemblyName is "?" ? null : [assemblyName];
        if (symbol is not INamespaceSymbol)
        {
            var namespaceName = VisitorHelper.GetId(symbol.ContainingNamespace);
            item.NamespaceName = string.IsNullOrEmpty(namespaceName) ? null : namespaceName;
        }

        var comment = symbol.GetDocumentationComment(_compilation, expandIncludes: true, expandInheritdoc: true);
        if (XmlComment.Parse(comment.FullXmlFragment, GetXmlCommentParserContext(item)) is { } commentModel)
        {
            item.Summary = commentModel.Summary;
            item.Remarks = commentModel.Remarks;
            item.Exceptions = commentModel.Exceptions;
            item.SeeAlsos = commentModel.SeeAlsos;
            item.Examples = commentModel.Examples;
            item.CommentModel = commentModel;
        }

        if (item.Exceptions != null)
        {
            foreach (var exceptions in item.Exceptions)
            {
                AddReference(exceptions.Type, exceptions.CommentId);
            }
        }

        if (item.SeeAlsos != null)
        {
            foreach (var i in item.SeeAlsos.Where(l => l.LinkType == LinkType.CRef))
            {
                AddReference(i.LinkId, i.CommentId);
            }
        }

        _generator.DefaultVisit(symbol, item);
        return item;
    }

    public override MetadataItem VisitAssembly(IAssemblySymbol symbol)
    {
        var item = new MetadataItem
        {
            Name = VisitorHelper.GetId(symbol),
            DisplayNames = new SortedList<SyntaxLanguage, string>
            {
                { SyntaxLanguage.Default, symbol.MetadataName },
            },
            DisplayQualifiedNames = new SortedList<SyntaxLanguage, string>
            {
                { SyntaxLanguage.Default, symbol.MetadataName },
            },
            Type = MemberType.Assembly,
        };

        IEnumerable<INamespaceSymbol> namespaces;
        if (!string.IsNullOrEmpty(VisitorHelper.GlobalNamespaceId))
        {
            namespaces = Enumerable.Repeat(symbol.GlobalNamespace, 1);
        }
        else
        {
            namespaces = symbol.GlobalNamespace.GetNamespaceMembers();
        }

        item.Items = VisitDescendants(
            namespaces,
            ns => ns.GetMembers().OfType<INamespaceSymbol>(),
            ns => ns.GetMembers().OfType<INamedTypeSymbol>().Any(t => _filter.IncludeApi(t)));
        item.References = _references;
        return item;
    }

    public override MetadataItem VisitNamespace(INamespaceSymbol symbol)
    {
        var item = DefaultVisit(symbol);
        if (item == null)
        {
            return null;
        }
        item.Type = MemberType.Namespace;
        item.Items = VisitDescendants(
            symbol.GetMembers().OfType<ITypeSymbol>(),
            t => t.GetMembers().OfType<ITypeSymbol>(),
            t => true);
        AddReference(symbol);
        return item;
    }

    public override MetadataItem VisitNamedType(INamedTypeSymbol symbol)
    {
        var item = DefaultVisit(symbol);
        if (item == null)
        {
            return null;
        }

        GenerateInheritance(symbol, item);

        if (!symbol.IsStatic)
        {
            GenerateExtensionMethods(symbol, item);
        }

        item.Type = VisitorHelper.GetMemberTypeFromTypeKind(symbol.TypeKind);
        item.Syntax ??= new SyntaxDetail { Content = [] };
        if (item.Syntax.Content == null)
        {
            item.Syntax.Content = [];
        }
        _generator.GenerateSyntax(symbol, item.Syntax, _filter);

        if (symbol.TypeParameters.Length > 0)
        {
            if (item.Syntax.TypeParameters == null)
            {
                item.Syntax.TypeParameters = [];
            }

            foreach (var p in symbol.TypeParameters)
            {
                var param = VisitorHelper.GetTypeParameterDescription(p, item);
                item.Syntax.TypeParameters.Add(param);
            }
        }

        if (symbol.TypeKind == TypeKind.Delegate)
        {
            var typeGenericParameters = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
            AddMethodSyntax(symbol.DelegateInvokeMethod, item, typeGenericParameters, EmptyListOfString);
        }

        item.Items = [];
        foreach (
            var member in symbol.GetMembers()
            .Where(static s =>
                s is not INamedTypeSymbol
                && !s.Name.StartsWith('<')
                && (s is not IMethodSymbol ms || ms.MethodKind != MethodKind.StaticConstructor)
            ))
        {
            var memberItem = member.Accept(this);
            if (memberItem != null)
            {
                item.Items.Add(memberItem);
            }
        }

        AddReference(symbol);

        item.Attributes = GetAttributeInfo(symbol.GetAttributes());

        return item;
    }

    public override MetadataItem VisitMethod(IMethodSymbol symbol)
    {
        MetadataItem result = GetYamlItem(symbol);
        if (result == null)
        {
            return null;
        }
        result.Syntax ??= new SyntaxDetail { Content = [] };

        if (symbol.TypeParameters.Length > 0)
        {
            if (result.Syntax.TypeParameters == null)
            {
                result.Syntax.TypeParameters = [];
            }

            foreach (var p in symbol.TypeParameters)
            {
                var param = VisitorHelper.GetTypeParameterDescription(p, result);
                result.Syntax.TypeParameters.Add(param);
            }
        }

        var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

        var methodGenericParameters = symbol.IsGenericMethod ? (from p in symbol.TypeParameters select p.Name).ToList() : EmptyListOfString;

        AddMethodSyntax(symbol, result, typeGenericParameters, methodGenericParameters);

        if (result.Syntax.Content == null)
        {
            result.Syntax.Content = [];
        }
        _generator.GenerateSyntax(symbol, result.Syntax, _filter);

        if (symbol is { IsOverride: true, OverriddenMethod: not null })
        {
            result.Overridden = AddSpecReference(symbol.OverriddenMethod, typeGenericParameters, methodGenericParameters);
        }

        result.Overload = AddOverloadReference(symbol.OriginalDefinition);

        AddMemberImplements(symbol, result, typeGenericParameters, methodGenericParameters);

        result.Attributes = GetAttributeInfo(symbol.GetAttributes());

        result.IsExplicitInterfaceImplementation = !symbol.ExplicitInterfaceImplementations.IsEmpty;
        result.IsExtensionMethod = symbol.IsExtensionMethod;

        return result;
    }

    public override MetadataItem VisitField(IFieldSymbol symbol)
    {
        MetadataItem result = GetYamlItem(symbol);
        if (result == null)
        {
            return null;
        }
        result.Syntax ??= new SyntaxDetail { Content = [] };
        if (result.Syntax.Content == null)
        {
            result.Syntax.Content = [];
        }
        _generator.GenerateSyntax(symbol, result.Syntax, _filter);

        var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

        var id = AddSpecReference(symbol.Type, typeGenericParameters);
        result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true);
        Debug.Assert(result.Syntax.Return.Type != null);

        result.Attributes = GetAttributeInfo(symbol.GetAttributes());

        return result;
    }

    public override MetadataItem VisitEvent(IEventSymbol symbol)
    {
        MetadataItem result = GetYamlItem(symbol);
        if (result == null)
        {
            return null;
        }
        result.Syntax ??= new SyntaxDetail { Content = [] };
        if (result.Syntax.Content == null)
        {
            result.Syntax.Content = [];
        }
        _generator.GenerateSyntax(symbol, result.Syntax, _filter);

        var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

        if (symbol is { IsOverride: true, OverriddenEvent: not null })
        {
            result.Overridden = AddSpecReference(symbol.OverriddenEvent, typeGenericParameters);
        }

        var id = AddSpecReference(symbol.Type, typeGenericParameters);
        result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true);
        Debug.Assert(result.Syntax.Return.Type != null);

        AddMemberImplements(symbol, result, typeGenericParameters);

        result.Attributes = GetAttributeInfo(symbol.GetAttributes());

        result.IsExplicitInterfaceImplementation = !symbol.ExplicitInterfaceImplementations.IsEmpty;

        return result;
    }

    public override MetadataItem VisitProperty(IPropertySymbol symbol)
    {
        MetadataItem result = GetYamlItem(symbol);
        if (result == null)
        {
            return null;
        }
        result.Syntax ??= new SyntaxDetail { Content = [] };
        if (result.Syntax.Parameters == null)
        {
            result.Syntax.Parameters = [];
        }
        if (result.Syntax.Content == null)
        {
            result.Syntax.Content = [];
        }
        _generator.GenerateSyntax(symbol, result.Syntax, _filter);

        var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

        if (symbol.Parameters.Length > 0)
        {
            foreach (var p in symbol.Parameters)
            {
                var id = AddSpecReference(p.Type, typeGenericParameters);
                var param = VisitorHelper.GetParameterDescription(p, result, id, false);
                Debug.Assert(param.Type != null);
                result.Syntax.Parameters.Add(param);
            }
        }
        {
            var id = AddSpecReference(symbol.Type, typeGenericParameters);
            result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true);
            Debug.Assert(result.Syntax.Return.Type != null);
        }

        if (symbol is { IsOverride: true, OverriddenProperty: not null })
        {
            result.Overridden = AddSpecReference(symbol.OverriddenProperty, typeGenericParameters);
        }

        result.Overload = AddOverloadReference(symbol.OriginalDefinition);

        AddMemberImplements(symbol, result, typeGenericParameters);

        result.Attributes = GetAttributeInfo(symbol.GetAttributes());

        result.IsExplicitInterfaceImplementation = !symbol.ExplicitInterfaceImplementations.IsEmpty;

        return result;
    }

    public string AddReference(ISymbol symbol)
    {
        var memberType = GetMemberTypeFromSymbol(symbol);
        if (memberType == MemberType.Default)
        {
            Debug.Fail("Unexpected member type.");
            throw new InvalidOperationException("Unexpected member type.");
        }
        return _generator.AddReference(symbol, _references, _filter);
    }

    public string AddReference(string id, string commentId)
    {
        if (_references.ContainsKey(id))
            return id;

        var reference = new ReferenceItem { CommentId = commentId };
        if (DocumentationCommentId.GetFirstSymbolForDeclarationId(commentId, _compilation) is { } symbol)
        {
            reference.NameParts = [];
            reference.NameWithTypeParts = [];
            reference.QualifiedNameParts = [];
            reference.IsDefinition = symbol.IsDefinition;

            _generator.GenerateReference(symbol, reference, asOverload: false, _filter);
        }

        _references[id] = reference;
        return id;
    }

    public string AddOverloadReference(ISymbol symbol)
    {
        var memberType = GetMemberTypeFromSymbol(symbol);
        switch (memberType)
        {
            case MemberType.Property:
            case MemberType.Constructor:
            case MemberType.Method:
            case MemberType.Operator:
                return _generator.AddOverloadReference(symbol, _references, _filter);
            default:
                Debug.Fail("Unexpected member type.");
                throw new InvalidOperationException("Unexpected member type.");
        }
    }

    public string AddSpecReference(
        ISymbol symbol,
        IReadOnlyList<string> typeGenericParameters = null,
        IReadOnlyList<string> methodGenericParameters = null)
    {
        return _generator.AddSpecReference(symbol, typeGenericParameters, methodGenericParameters, _references, _filter);
    }

    private static MemberType GetMemberTypeFromSymbol(ISymbol symbol)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Namespace:
                return MemberType.Namespace;
            case SymbolKind.NamedType:
                INamedTypeSymbol nameTypeSymbol = symbol as INamedTypeSymbol;
                Debug.Assert(nameTypeSymbol != null);
                if (nameTypeSymbol != null)
                {
                    return VisitorHelper.GetMemberTypeFromTypeKind(nameTypeSymbol.TypeKind);
                }
                else
                {
                    return MemberType.Default;
                }
            case SymbolKind.Event:
                return MemberType.Event;
            case SymbolKind.Field:
                return MemberType.Field;
            case SymbolKind.Property:
                return MemberType.Property;
            case SymbolKind.Method:
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    Debug.Assert(methodSymbol != null);
                    if (methodSymbol == null) return MemberType.Default;
                    switch (methodSymbol.MethodKind)
                    {
                        case MethodKind.AnonymousFunction:
                        case MethodKind.DelegateInvoke:
                        case MethodKind.Destructor:
                        case MethodKind.ExplicitInterfaceImplementation:
                        case MethodKind.Ordinary:
                        case MethodKind.ReducedExtension:
                        case MethodKind.DeclareMethod:
                            return MemberType.Method;
                        case MethodKind.BuiltinOperator:
                        case MethodKind.UserDefinedOperator:
                        case MethodKind.Conversion:
                            return MemberType.Operator;
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            return MemberType.Constructor;
                        // ignore: Property's get/set, and event's add/remove/raise
                        case MethodKind.PropertyGet:
                        case MethodKind.PropertySet:
                        case MethodKind.EventAdd:
                        case MethodKind.EventRemove:
                        case MethodKind.EventRaise:
                        default:
                            return MemberType.Default;
                    }
                }
            default:
                return MemberType.Default;
        }
    }

    private MetadataItem GetYamlItem(ISymbol symbol)
    {
        var item = DefaultVisit(symbol);
        if (item == null) return null;
        item.Type = GetMemberTypeFromSymbol(symbol);
        if (item.Type == MemberType.Default)
        {
            // If Default, then it is Property get/set or Event add/remove/raise, ignore
            return null;
        }

        return item;
    }

    private List<MetadataItem> VisitDescendants<T>(
        IEnumerable<T> children,
        Func<T, IEnumerable<T>> getChildren,
        Func<T, bool> filter)
        where T : ISymbol
    {
        var result = new List<MetadataItem>();
        var stack = new Stack<T>(children.Reverse());
        while (stack.Count > 0)
        {
            var child = stack.Pop();
            if (filter(child))
            {
                var item = child.Accept(this);
                if (item != null)
                {
                    result.Add(item);
                }
            }
            foreach (var m in getChildren(child).Reverse())
            {
                stack.Push(m);
            }
        }
        return result;
    }

    private static bool IsInheritable(ISymbol memberSymbol)
    {
        var kind = (memberSymbol as IMethodSymbol)?.MethodKind;
        if (kind != null)
        {
            switch (kind.Value)
            {
                case MethodKind.ExplicitInterfaceImplementation:
                case MethodKind.DeclareMethod:
                case MethodKind.Ordinary:
                    return true;
                default:
                    return false;
            }
        }
        return true;
    }

    private void GenerateInheritance(INamedTypeSymbol symbol, MetadataItem item)
    {
        Dictionary<string, string> dict = null;
        if (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Struct)
        {
            var type = symbol;
            var inheritance = new List<string>();
            dict = [];
            var typeParameterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
            while (type != null)
            {
                // TODO: special handles for errorType: change to System.Object
                if (type.Kind == SymbolKind.ErrorType)
                {
                    inheritance.Add("System.Object");
                    break;
                }

                if (!type.Equals(symbol, SymbolEqualityComparer.Default))
                {
                    inheritance.Add(AddSpecReference(type, typeParameterNames));
                }

                AddInheritedMembers(symbol, type, dict, typeParameterNames);
                type = type.BaseType;
            }

            if (symbol.TypeKind == TypeKind.Class)
            {
                inheritance.Reverse();
                item.Inheritance = inheritance;
            }
            if (symbol.AllInterfaces.Length > 0)
            {
                item.Implements = (from t in symbol.AllInterfaces
                                   where _filter.IncludeApi(t)
                                   select AddSpecReference(t, typeParameterNames)).ToList();
                if (item.Implements.Count == 0)
                {
                    item.Implements = null;
                }
            }
        }
        else if (symbol.TypeKind == TypeKind.Interface)
        {
            dict = [];
            var typeParameterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
            AddInheritedMembers(symbol, symbol, dict, typeParameterNames);
            for (int i = 0; i < symbol.AllInterfaces.Length; i++)
            {
                AddInheritedMembers(symbol, symbol.AllInterfaces[i], dict, typeParameterNames);
            }
        }
        if (dict != null)
        {
            var inheritedMembers = (from r in dict.Values where r != null select r).ToList();
            item.InheritedMembers = inheritedMembers.Count > 0 ? inheritedMembers : null;
        }
    }

    private void AddMemberImplements(ISymbol symbol, MetadataItem item, IReadOnlyList<string> typeGenericParameters = null)
    {
        if (symbol.ContainingType.AllInterfaces.Length <= 0) return;
        item.Implements = (from type in symbol.ContainingType.AllInterfaces
                           where _filter.IncludeApi(type)
                           from member in type.GetMembers()
                           where _filter.IncludeApi(member)
                           where symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(member), SymbolEqualityComparer.Default)
                           select AddSpecReference(member, typeGenericParameters)).ToList();
        if (item.Implements.Count == 0)
        {
            item.Implements = null;
        }
    }

    private void AddMemberImplements(IMethodSymbol symbol, MetadataItem item, IReadOnlyList<string> typeGenericParameters = null, IReadOnlyList<string> methodGenericParameters = null)
    {
        if (symbol.ContainingType.AllInterfaces.Length <= 0) return;
        item.Implements = (from type in symbol.ContainingType.AllInterfaces
                           where _filter.IncludeApi(type)
                           from member in type.GetMembers()
                           where _filter.IncludeApi(member)
                           where symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(member), SymbolEqualityComparer.Default)
                           select AddSpecReference(
                               symbol.TypeParameters.Length == 0 ? member : ((IMethodSymbol)member).Construct(symbol.TypeParameters.ToArray<ITypeSymbol>()),
                               typeGenericParameters,
                               methodGenericParameters)).ToList();
        if (item.Implements.Count == 0)
        {
            item.Implements = null;
        }
    }

    private void GenerateExtensionMethods(INamedTypeSymbol symbol, MetadataItem item)
    {
        var extensions = new List<string>();
        foreach (var extensionMethod in _extensionMethods.Where(p => p.Language == symbol.Language))
        {
            var reduced = extensionMethod.ReduceExtensionMethod(symbol);
            if (reduced != null)
            {
                // update reference
                // Roslyn could get the instantiated type. e.g.
                // <code>
                // public class Foo<T> {}
                // public class FooImple<T> : Foo<Foo<T[]>> {}
                // public static class Extension { public static void Play<Tool, Way>(this Foo<Tool> foo, Tool t, Way w) {} }
                // </code>
                // Roslyn generated id for the reduced extension method of FooImple<T> is like "Play``2(Foo{`0[]},``1)"
                var typeParameterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
                var methodGenericParameters = reduced.IsGenericMethod ? (from p in reduced.TypeParameters select p.Name).ToList() : EmptyListOfString;
                var id = AddSpecReference(reduced, typeParameterNames, methodGenericParameters);

                extensions.Add(id);
            }
        }
        extensions.Sort();
        item.ExtensionMethods = extensions.Count > 0 ? extensions : null;
    }

    private void AddInheritedMembers(INamedTypeSymbol symbol, INamedTypeSymbol type, Dictionary<string, string> dict, IReadOnlyList<string> typeParameterNames)
    {
        foreach (var m in from m in type.GetMembers()
                          where m is not INamedTypeSymbol
                          where _filter.IncludeApi(m)
                          where m.DeclaredAccessibility is Accessibility.Public || !(symbol.IsSealed || symbol.TypeKind is TypeKind.Struct)
                          where IsInheritable(m)
                          select m)
        {
            var sig = MemberSigRegex().Replace(SpecIdHelper.GetSpecId(m, typeParameterNames), string.Empty);
            if (!dict.ContainsKey(sig))
            {
                dict.Add(sig, type.Equals(symbol, SymbolEqualityComparer.Default) ? null : AddSpecReference(m, typeParameterNames));
            }
        }
    }

    private void AddMethodSyntax(IMethodSymbol symbol, MetadataItem result, IReadOnlyList<string> typeGenericParameters, IReadOnlyList<string> methodGenericParameters)
    {
        if (!symbol.ReturnsVoid)
        {
            var id = AddSpecReference(symbol.ReturnType, typeGenericParameters, methodGenericParameters);
            result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true);
            result.Syntax.Return.Attributes = GetAttributeInfo(symbol.GetReturnTypeAttributes());
        }

        if (symbol.Parameters.Length > 0)
        {
            if (result.Syntax.Parameters == null)
            {
                result.Syntax.Parameters = [];
            }

            foreach (var p in symbol.Parameters)
            {
                var id = AddSpecReference(p.Type, typeGenericParameters, methodGenericParameters);
                var param = VisitorHelper.GetParameterDescription(p, result, id, false);
                Debug.Assert(param.Type != null);
                param.Attributes = GetAttributeInfo(p.GetAttributes());
                result.Syntax.Parameters.Add(param);
            }
        }
    }

    private XmlCommentParserContext GetXmlCommentParserContext(MetadataItem item)
    {
        return new XmlCommentParserContext
        {
            SkipMarkup = _config.ShouldSkipMarkup,
            AddReferenceDelegate = AddReferenceDelegate,
            Source = item.Source,
            ResolveCode = ResolveCode,
        };

        void AddReferenceDelegate(string id, string commentId)
        {
            var r = AddReference(id, commentId);
            item.References ??= [];

            // only record the id now, the value would be fed at later phase after merge
            item.References[id] = null;
        }

        string ResolveCode(string source)
        {
            var basePath = _config.CodeSourceBasePath ?? (
                item.Source?.Path is { } sourcePath
                    ? Path.GetDirectoryName(Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, sourcePath)))
                    : null);

            var path = Path.GetFullPath(Path.Combine(basePath, source));
            if (!File.Exists(path))
            {
                Logger.LogWarning($"Source file '{path}' not found.", code: "CodeNotFound");
                return null;
            }

            return File.ReadAllText(path);
        }
    }

    private List<AttributeInfo> GetAttributeInfo(ImmutableArray<AttributeData> attributes)
    {
        if (attributes.Length == 0)
        {
            return null;
        }
        var result =
            (from attr in attributes
             where attr.AttributeClass is not IErrorTypeSymbol
             where attr.AttributeConstructor != null
             where _filter.IncludeAttribute(attr.AttributeConstructor)
             select new AttributeInfo
             {
                 Type = AddSpecReference(attr.AttributeClass),
                 Constructor = AddSpecReference(attr.AttributeConstructor),
                 Arguments = GetArguments(attr),
                 NamedArguments = GetNamedArguments(attr)
             } into attr
             where attr.Arguments != null
             select attr).ToList();
        if (result.Count == 0)
        {
            return null;
        }
        return result;
    }

    private List<ArgumentInfo> GetArguments(AttributeData attr)
    {
        var result = new List<ArgumentInfo>();
        foreach (var arg in attr.ConstructorArguments)
        {
            var argInfo = GetArgumentInfo(arg);
            if (argInfo == null)
            {
                return null;
            }
            result.Add(argInfo);
        }
        return result;
    }

    private ArgumentInfo GetArgumentInfo(TypedConstant arg)
    {
        var result = new ArgumentInfo();
        if (arg.Type.TypeKind == TypeKind.Array)
        {
            // todo : value of array.
            return null;
        }
        if (arg.Value != null)
        {
            if (arg.Value is ITypeSymbol type)
            {
                if (!_filter.IncludeApi(type))
                {
                    return null;
                }
            }
            result.Value = GetConstantValueForArgumentInfo(arg);
        }
        result.Type = AddSpecReference(arg.Type);
        return result;
    }

    private object GetConstantValueForArgumentInfo(TypedConstant arg)
    {
        if (arg.Value is ITypeSymbol type)
        {
            return AddSpecReference(type);
        }

        switch (Convert.GetTypeCode(arg.Value))
        {
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                // work around: yaml cannot deserialize them.
                return arg.Value.ToString();
            default:
                return arg.Value;
        }
    }

    private List<NamedArgumentInfo> GetNamedArguments(AttributeData attr)
    {
        var result =
            (from pair in attr.NamedArguments
             select GetNamedArgumentInfo(pair) into namedArgument
             where namedArgument != null
             select namedArgument).ToList();
        if (result.Count == 0)
        {
            return null;
        }
        return result;
    }

    private NamedArgumentInfo GetNamedArgumentInfo(KeyValuePair<string, TypedConstant> pair)
    {
        var result = new NamedArgumentInfo
        {
            Name = pair.Key,
        };
        var arg = pair.Value;
        if (arg.Type.TypeKind == TypeKind.Array)
        {
            // todo : value of array.
            return null;
        }
        else if (arg.Value != null)
        {
            if (arg.Value is ITypeSymbol type)
            {
                if (!_filter.IncludeApi(type))
                {
                    return null;
                }
            }
            result.Value = GetConstantValueForArgumentInfo(arg);
        }
        result.Type = AddSpecReference(arg.Type);
        return result;
    }
}
