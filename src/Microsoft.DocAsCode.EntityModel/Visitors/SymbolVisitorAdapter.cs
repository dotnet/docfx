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

    public class SymbolVisitorAdapter
        : SymbolVisitor<MetadataItem>
    {
        #region Fields
        private static readonly Regex MemberSigRegex = new Regex(@"^([\w\{\}`]+\.)+", RegexOptions.Compiled);
        private static readonly IReadOnlyList<string> EmptyListOfString = new string[0];
        private readonly YamlModelGenerator _generator;
        private Dictionary<string, ReferenceItem> _references;
        public bool _preserveRawInlineComments;

        #endregion

        #region Constructor

        public SymbolVisitorAdapter(YamlModelGenerator generator, SyntaxLanguage language, bool preserveRawInlineComments = false)
        {
            _generator = generator;
            Language = language;
            _preserveRawInlineComments = preserveRawInlineComments;
        }

        #endregion

        #region Properties

        public SyntaxLanguage Language { get; private set; }

        #endregion

        #region Overrides

        public override MetadataItem DefaultVisit(ISymbol symbol)
        {
            if (!VisitorHelper.CanVisit(symbol))
            {
                return null;
            }
            var item = new MetadataItem
            {
                Name = VisitorHelper.GetId(symbol),
                RawComment = symbol.GetDocumentationCommentXml(),
                Language = Language,
            };

            item.DisplayNames = new SortedList<SyntaxLanguage, string>();
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
                    AddReference(exceptions.Type);
                }
            }

            if (item.Sees != null)
            {
                foreach (var i in item.Sees)
                {
                    AddReference(i.Type);
                }
            }

            if (item.SeeAlsos != null)
            {
                foreach (var i in item.SeeAlsos)
                {
                    AddReference(i.Type);
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
            item.Items = VisitDescendants(
                symbol.GlobalNamespace.GetNamespaceMembers(),
                ns => ns.GetMembers().OfType<INamespaceSymbol>(),
                ns => ns.GetMembers().OfType<INamedTypeSymbol>().Any(t => VisitorHelper.CanVisit(t)));
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
            AddSpecReference(symbol.Type, typeGenericParameters);

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

            AddSpecReference(symbol.Type, typeGenericParameters);

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

            _generator.GenerateProperty(symbol, result, this);

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

        public string AddReference(string id)
        {
            return _generator.AddReference(id, _references);
        }

        public string AddSpecReference(
            ISymbol symbol,
            IReadOnlyList<string> typeGenericParameters = null,
            IReadOnlyList<string> methodGenericParameters = null)
        {
            return _generator.AddSpecReference(symbol, typeGenericParameters, methodGenericParameters, _references, this);
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

            var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            Debug.Assert(syntaxRef != null || item.Type == MemberType.Constructor);
            if (syntaxRef == null)
            {
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
                                       where VisitorHelper.CanVisit(t)
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

        private void AddInheritedMembers(INamedTypeSymbol symbol, INamedTypeSymbol type, Dictionary<string, string> dict, IReadOnlyList<string> typeParamterNames)
        {
            foreach (var m in from m in type.GetMembers()
                              where !(m is INamedTypeSymbol)
                              where VisitorHelper.CanVisit(m, symbol == type || !symbol.IsSealed || symbol.TypeKind != TypeKind.Struct)
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
                    result.Syntax.Parameters.Add(param);
                }
            }
        }

        private ITripleSlashCommentParserContext GetTripleSlashCommentParserContext(MetadataItem item, bool preserve)
        {
            return new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = GetAddReferenceDelegate(item),
                Normalize = true,
                PreserveRawInlineComments = preserve
            };
        }

        private Action<string> GetAddReferenceDelegate(MetadataItem item)
        {
            return id =>
            {
                AddReference(id);
                if (item.References == null)
                {
                    item.References = new Dictionary<string, ReferenceItem>();
                }
                item.References[id] = null;
            };
        }

        #endregion
    }
}
