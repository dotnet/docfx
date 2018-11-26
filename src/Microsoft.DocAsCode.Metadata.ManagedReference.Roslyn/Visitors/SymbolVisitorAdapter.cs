// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Exceptions;

    public class SymbolVisitorAdapter
        : SymbolVisitor<MetadataItem>
    {
        #region Fields
        private static readonly Regex MemberSigRegex = new Regex(@"^([\w\{\}`]+\.)+", RegexOptions.Compiled);
        private static readonly IReadOnlyList<string> EmptyListOfString = new string[0];
        private readonly YamlModelGenerator _generator;
        private Dictionary<string, ReferenceItem> _references;
        private bool _preserveRawInlineComments;
        private readonly IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> _extensionMethods;
        private readonly Compilation _currentCompilation;
        private readonly CompilationReference _currentCompilationRef;
        private readonly string _codeSourceBasePath;

        #endregion

        #region Constructor

        public SymbolVisitorAdapter(YamlModelGenerator generator, SyntaxLanguage language, Compilation compilation, ExtractMetadataOptions options)
        {
            _generator = generator;
            Language = language;
            _currentCompilation = compilation;
            _currentCompilationRef = compilation.ToMetadataReference();
            _preserveRawInlineComments = options.PreserveRawInlineComments;
            var configFilterRule = ConfigFilterRule.LoadWithDefaults(options.FilterConfigFile);
            var filterVisitor = options.DisableDefaultFilter ? (IFilterVisitor)new AllMemberFilterVisitor() : new DefaultFilterVisitor();
            FilterVisitor = filterVisitor.WithConfig(configFilterRule).WithCache();
            _extensionMethods = options.RoslynExtensionMethods != null ? options.RoslynExtensionMethods.ToDictionary(p => p.Key, p => p.Value.Where(e => FilterVisitor.CanVisitApi(e))) : new Dictionary<Compilation, IEnumerable<IMethodSymbol>>();
            _codeSourceBasePath = options.CodeSourceBasePath;
        }

        #endregion

        #region Properties

        public SyntaxLanguage Language { get; private set; }

        public IFilterVisitor FilterVisitor { get; private set; }

        #endregion

        #region Overrides

        public override MetadataItem DefaultVisit(ISymbol symbol)
        {
            if (!FilterVisitor.CanVisitApi(symbol))
            {
                return null;
            }
            var item = new MetadataItem
            {
                Name = VisitorHelper.GetId(symbol),
                CommentId = VisitorHelper.GetCommentId(symbol),
                RawComment = symbol.GetDocumentationCommentXml(),
                Language = Language,
            };

            item.DisplayNames = new SortedList<SyntaxLanguage, string>();
            item.DisplayNamesWithType = new SortedList<SyntaxLanguage, string>();
            item.DisplayQualifiedNames = new SortedList<SyntaxLanguage, string>();
            item.Source = VisitorHelper.GetSourceDetail(symbol);
            var assemblyName = symbol.ContainingAssembly?.Name;
            item.AssemblyNameList = string.IsNullOrEmpty(assemblyName) ? null : new List<string> { assemblyName };
            if (!(symbol is INamespaceSymbol))
            {
                var namespaceName = VisitorHelper.GetId(symbol.ContainingNamespace);
                item.NamespaceName = string.IsNullOrEmpty(namespaceName) ? null : namespaceName;
            }

            VisitorHelper.FeedComments(item, GetTripleSlashCommentParserContext(item, _preserveRawInlineComments));
            if (item.Exceptions != null)
            {
                foreach (var exceptions in item.Exceptions)
                {
                    AddReference(exceptions.Type, exceptions.CommentId);
                }
            }

            if (item.Sees != null)
            {
                foreach (var i in item.Sees.Where(l => l.LinkType == LinkType.CRef))
                {
                    AddReference(i.LinkId, i.CommentId);
                }
            }

            if (item.SeeAlsos != null)
            {
                foreach (var i in item.SeeAlsos.Where(l => l.LinkType == LinkType.CRef))
                {
                    AddReference(i.LinkId, i.CommentId);
                }
            }

            _generator.DefaultVisit(symbol, item, this);
            return item;
        }

        public override MetadataItem VisitAssembly(IAssemblySymbol symbol)
        {
            var item = new MetadataItem
            {
                Name = VisitorHelper.GetId(symbol),
                RawComment = symbol.GetDocumentationCommentXml(),
                Language = Language,
            };

            item.DisplayNames = new SortedList<SyntaxLanguage, string>
            {
                { SyntaxLanguage.Default, symbol.MetadataName },
            };
            item.DisplayQualifiedNames = new SortedList<SyntaxLanguage, string>
            {
                { SyntaxLanguage.Default, symbol.MetadataName },
            };
            item.Type = MemberType.Assembly;
            _references = new Dictionary<string, ReferenceItem>();

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
                ns => ns.GetMembers().OfType<INamedTypeSymbol>().Any(t => FilterVisitor.CanVisitApi(t)));
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
            if (item.Syntax == null)
            {
                item.Syntax = new SyntaxDetail { Content = new SortedList<SyntaxLanguage, string>() };
            }
            if (item.Syntax.Content == null)
            {
                item.Syntax.Content = new SortedList<SyntaxLanguage, string>();
            }
            _generator.GenerateSyntax(item.Type, symbol, item.Syntax, this);

            if (symbol.TypeParameters.Length > 0)
            {
                if (item.Syntax.TypeParameters == null)
                {
                    item.Syntax.TypeParameters = new List<ApiParameter>();
                }

                foreach (var p in symbol.TypeParameters)
                {
                    var param = VisitorHelper.GetTypeParameterDescription(p, item, GetTripleSlashCommentParserContext(item, _preserveRawInlineComments));
                    item.Syntax.TypeParameters.Add(param);
                }
            }

            if (symbol.TypeKind == TypeKind.Delegate)
            {
                var typeGenericParameters = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
                AddMethodSyntax(symbol.DelegateInvokeMethod, item, typeGenericParameters, EmptyListOfString);
            }

            _generator.GenerateNamedType(symbol, item, this);

            item.Items = new List<MetadataItem>();
            foreach (var member in symbol.GetMembers().Where(s => !(s is INamedTypeSymbol)))
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
            if (result.Syntax == null)
            {
                result.Syntax = new SyntaxDetail { Content = new SortedList<SyntaxLanguage, string>() };
            }

            if (symbol.TypeParameters.Length > 0)
            {
                if (result.Syntax.TypeParameters == null)
                {
                    result.Syntax.TypeParameters = new List<ApiParameter>();
                }

                foreach (var p in symbol.TypeParameters)
                {
                    var param = VisitorHelper.GetTypeParameterDescription(p, result, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
                    result.Syntax.TypeParameters.Add(param);
                }
            }

            var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

            var methodGenericParameters = symbol.IsGenericMethod ? (from p in symbol.TypeParameters select p.Name).ToList() : EmptyListOfString;

            AddMethodSyntax(symbol, result, typeGenericParameters, methodGenericParameters);

            if (result.Syntax.Content == null)
            {
                result.Syntax.Content = new SortedList<SyntaxLanguage, string>();
            }
            _generator.GenerateSyntax(result.Type, symbol, result.Syntax, this);

            _generator.GenerateMethod(symbol, result, this);

            if (symbol.IsOverride && symbol.OverriddenMethod != null)
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
            if (result.Syntax == null)
            {
                result.Syntax = new SyntaxDetail { Content = new SortedList<SyntaxLanguage, string>() };
            }
            if (result.Syntax.Content == null)
            {
                result.Syntax.Content = new SortedList<SyntaxLanguage, string>();
            }
            _generator.GenerateSyntax(result.Type, symbol, result.Syntax, this);
            _generator.GenerateField(symbol, result, this);

            var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

            var id = AddSpecReference(symbol.Type, typeGenericParameters);
            result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
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
            if (result.Syntax == null)
            {
                result.Syntax = new SyntaxDetail { Content = new SortedList<SyntaxLanguage, string>() };
            }
            if (result.Syntax.Content == null)
            {
                result.Syntax.Content = new SortedList<SyntaxLanguage, string>();
            }
            _generator.GenerateSyntax(result.Type, symbol, result.Syntax, this);
            _generator.GenerateEvent(symbol, result, this);

            var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

            if (symbol.IsOverride && symbol.OverriddenEvent != null)
            {
                result.Overridden = AddSpecReference(symbol.OverriddenEvent, typeGenericParameters);
            }

            var id = AddSpecReference(symbol.Type, typeGenericParameters);
            result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
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
            if (result.Syntax == null)
            {
                result.Syntax = new SyntaxDetail { Content = new SortedList<SyntaxLanguage, string>() };
            }
            if (result.Syntax.Parameters == null)
            {
                result.Syntax.Parameters = new List<ApiParameter>();
            }
            if (result.Syntax.Content == null)
            {
                result.Syntax.Content = new SortedList<SyntaxLanguage, string>();
            }
            _generator.GenerateSyntax(result.Type, symbol, result.Syntax, this);

            var typeGenericParameters = symbol.ContainingType.IsGenericType ? symbol.ContainingType.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;

            if (symbol.Parameters.Length > 0)
            {
                foreach (var p in symbol.Parameters)
                {
                    var id = AddSpecReference(p.Type, typeGenericParameters);
                    var param = VisitorHelper.GetParameterDescription(p, result, id, false, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
                    Debug.Assert(param.Type != null);
                    result.Syntax.Parameters.Add(param);
                }
            }
            {
                var id = AddSpecReference(symbol.Type, typeGenericParameters);
                result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
                Debug.Assert(result.Syntax.Return.Type != null);
            }

            if (symbol.IsOverride && symbol.OverriddenProperty != null)
            {
                result.Overridden = AddSpecReference(symbol.OverriddenProperty, typeGenericParameters);
            }

            result.Overload = AddOverloadReference(symbol.OriginalDefinition);

            _generator.GenerateProperty(symbol, result, this);

            AddMemberImplements(symbol, result, typeGenericParameters);

            result.Attributes = GetAttributeInfo(symbol.GetAttributes());

            result.IsExplicitInterfaceImplementation = !symbol.ExplicitInterfaceImplementations.IsEmpty;

            return result;
        }

        #endregion

        #region Public Methods

        public string AddReference(ISymbol symbol)
        {
            var memberType = GetMemberTypeFromSymbol(symbol);
            if (memberType == MemberType.Default)
            {
                Debug.Fail("Unexpected membertype.");
                throw new InvalidOperationException("Unexpected membertype.");
            }
            return _generator.AddReference(symbol, _references, this);
        }

        public string AddReference(string id, string commentId)
        {
            return _generator.AddReference(id, commentId, _references);
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
                    return _generator.AddOverloadReference(symbol, _references, this);
                default:
                    Debug.Fail("Unexpected membertype.");
                    throw new InvalidOperationException("Unexpected membertype.");
            }
        }

        public string AddSpecReference(
            ISymbol symbol,
            IReadOnlyList<string> typeGenericParameters = null,
            IReadOnlyList<string> methodGenericParameters = null)
        {
            try
            {
                return _generator.AddSpecReference(symbol, typeGenericParameters, methodGenericParameters, _references, this);
            }
            catch (Exception ex)
            {
                throw new DocfxException($"Unable to generate spec reference for {VisitorHelper.GetCommentId(symbol)}", ex);
            }
        }

        #endregion

        #region Private Methods

        private MemberType GetMemberTypeFromSymbol(ISymbol symbol)
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

        private bool IsInheritable(ISymbol memberSymbol)
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
                dict = new Dictionary<string, string>();
                var typeParamterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
                while (type != null)
                {
                    // TODO: special handles for errorType: change to System.Object
                    if (type.Kind == SymbolKind.ErrorType)
                    {
                        inheritance.Add("System.Object");
                        break;
                    }

                    if (type != symbol)
                    {
                        inheritance.Add(AddSpecReference(type, typeParamterNames));
                    }

                    AddInheritedMembers(symbol, type, dict, typeParamterNames);
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
                                       where FilterVisitor.CanVisitApi(t)
                                       select AddSpecReference(t, typeParamterNames)).ToList();
                    if (item.Implements.Count == 0)
                    {
                        item.Implements = null;
                    }
                }
            }
            else if (symbol.TypeKind == TypeKind.Interface)
            {
                dict = new Dictionary<string, string>();
                var typeParamterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
                AddInheritedMembers(symbol, symbol, dict, typeParamterNames);
                for (int i = 0; i < symbol.AllInterfaces.Length; i++)
                {
                    AddInheritedMembers(symbol, symbol.AllInterfaces[i], dict, typeParamterNames);
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
                               where FilterVisitor.CanVisitApi(type)
                               from member in type.GetMembers()
                               where FilterVisitor.CanVisitApi(member)
                               where symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(member))
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
                               where FilterVisitor.CanVisitApi(type)
                               from member in type.GetMembers()
                               where FilterVisitor.CanVisitApi(member)
                               where symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(member))
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
            foreach (var pair in _extensionMethods.Where(p => p.Key.Language == symbol.Language))
            {
                ITypeSymbol retargetedSymbol = symbol;

                // get retargeted symbol for cross-assembly case.
                if (pair.Key != _currentCompilation)
                {
                    var compilation = pair.Key.References.Any(r => r.Display == _currentCompilationRef.Display) ? pair.Key : pair.Key.AddReferences(new[] { _currentCompilationRef });
                    retargetedSymbol = compilation.FindSymbol<INamedTypeSymbol>(symbol);
                }
                if (retargetedSymbol == null)
                {
                    continue;
                }

                foreach (var e in pair.Value)
                {
                    var reduced = e.ReduceExtensionMethod(retargetedSymbol);
                    if ((object)reduced != null)
                    {
                        // update reference
                        // Roslyn could get the instaniated type. e.g.
                        // <code>
                        // public class Foo<T> {}
                        // public class FooImple<T> : Foo<Foo<T[]>> {}
                        // public static class Extension { public static void Play<Tool, Way>(this Foo<Tool> foo, Tool t, Way w) {} }
                        // </code>
                        // Roslyn generated id for the reduced extension method of FooImple<T> is like "Play``2(Foo{`0[]},``1)"
                        var typeParamterNames = symbol.IsGenericType ? symbol.Accept(TypeGenericParameterNameVisitor.Instance) : EmptyListOfString;
                        var methodGenericParameters = reduced.IsGenericMethod ? (from p in reduced.TypeParameters select p.Name).ToList() : EmptyListOfString;
                        var id = AddSpecReference(reduced, typeParamterNames, methodGenericParameters);

                        extensions.Add(id);
                    }
                }
            }
            item.ExtensionMethods = extensions.Count > 0 ? extensions : null;
        }

        private void AddInheritedMembers(INamedTypeSymbol symbol, INamedTypeSymbol type, Dictionary<string, string> dict, IReadOnlyList<string> typeParamterNames)
        {
            foreach (var m in from m in type.GetMembers()
                              where !(m is INamedTypeSymbol)
                              where FilterVisitor.CanVisitApi(m, symbol == type || !symbol.IsSealed || symbol.TypeKind != TypeKind.Struct)
                              where IsInheritable(m)
                              select m)
            {
                var sig = MemberSigRegex.Replace(SpecIdHelper.GetSpecId(m, typeParamterNames), string.Empty);
                if (!dict.ContainsKey(sig))
                {
                    dict.Add(sig, type == symbol ? null : AddSpecReference(m, typeParamterNames));
                }
            }
        }

        private void AddMethodSyntax(IMethodSymbol symbol, MetadataItem result, IReadOnlyList<string> typeGenericParameters, IReadOnlyList<string> methodGenericParameters)
        {
            if (!symbol.ReturnsVoid)
            {
                var id = AddSpecReference(symbol.ReturnType, typeGenericParameters, methodGenericParameters);
                result.Syntax.Return = VisitorHelper.GetParameterDescription(symbol, result, id, true, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
                result.Syntax.Return.Attributes = GetAttributeInfo(symbol.GetReturnTypeAttributes());
            }

            if (symbol.Parameters.Length > 0)
            {
                if (result.Syntax.Parameters == null)
                {
                    result.Syntax.Parameters = new List<ApiParameter>();
                }

                foreach (var p in symbol.Parameters)
                {
                    var id = AddSpecReference(p.Type, typeGenericParameters, methodGenericParameters);
                    var param = VisitorHelper.GetParameterDescription(p, result, id, false, GetTripleSlashCommentParserContext(result, _preserveRawInlineComments));
                    Debug.Assert(param.Type != null);
                    param.Attributes = GetAttributeInfo(p.GetAttributes());
                    result.Syntax.Parameters.Add(param);
                }
            }
        }

        private ITripleSlashCommentParserContext GetTripleSlashCommentParserContext(MetadataItem item, bool preserve)
        {
            return new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = GetAddReferenceDelegate(item),
                PreserveRawInlineComments = preserve,
                Source = item.Source,
                CodeSourceBasePath = _codeSourceBasePath
            };
        }

        private List<AttributeInfo> GetAttributeInfo(ImmutableArray<AttributeData> attributes)
        {
            if (attributes.Length == 0)
            {
                return null;
            }
            var result =
                (from attr in attributes
                 where !(attr.AttributeClass is IErrorTypeSymbol)
                 where attr.AttributeConstructor != null
                 where FilterVisitor.CanVisitAttribute(attr.AttributeConstructor)
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
                var type = arg.Value as ITypeSymbol;
                if (type != null)
                {
                    if (!FilterVisitor.CanVisitApi(type))
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
            var type = arg.Value as ITypeSymbol;
            if (type != null)
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
                var type = arg.Value as ITypeSymbol;
                if (type != null)
                {
                    if (!FilterVisitor.CanVisitApi(type))
                    {
                        return null;
                    }
                }
                result.Value = GetConstantValueForArgumentInfo(arg);
            }
            result.Type = AddSpecReference(arg.Type);
            return result;
        }

        private Action<string, string> GetAddReferenceDelegate(MetadataItem item)
        {
            return (id, commentId) =>
            {
                var r = AddReference(id, commentId);
                if (item.References == null)
                {
                    item.References = new Dictionary<string, ReferenceItem>();
                }

                // only record the id now, the value would be fed at later phase after merge
                item.References[id] = null;
            };
        }

        #endregion
    }
}
