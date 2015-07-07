namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class VisitorHelper
    {
        public static bool CanVisit(ISymbol symbol, bool wantProtectedMember = true)
        {
            if (symbol.DeclaredAccessibility == Accessibility.NotApplicable)
            {
                return true;
            }

            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                return CanVisitCore(methodSymbol, wantProtectedMember);
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return CanVisitCore(propertySymbol, wantProtectedMember);
            }

            var eventSymbol = symbol as IEventSymbol;
            if (eventSymbol != null)
            {
                return CanVisitCore(eventSymbol, wantProtectedMember);
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return CanVisitCore(fieldSymbol, wantProtectedMember);
            }

            var typeSymbol = symbol as INamedTypeSymbol;
            if (typeSymbol != null)
            {
                return CanVisitCore(typeSymbol, wantProtectedMember);
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            return true;
        }

        private static bool CanVisitCore(INamedTypeSymbol symbol, bool wantProtectedMember)
        {
            if (symbol.ContainingType != null)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        return CanVisit(symbol.ContainingType, wantProtectedMember);
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        return wantProtectedMember && CanVisit(symbol.ContainingType, wantProtectedMember);
                    default:
                        return false;
                }
            }
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool CanVisitCore(IMethodSymbol symbol, bool wantProtectedMember)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (CanVisit(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IPropertySymbol symbol, bool wantProtectedMember)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (CanVisit(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IEventSymbol symbol, bool wantProtectedMember)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtectedMember;
                default:
                    break;
            }
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
                {
                    if (CanVisit(symbol.ExplicitInterfaceImplementations[i].ContainingType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanVisitCore(IFieldSymbol symbol, bool wantProtected)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return wantProtected;
                default:
                    break;
            }
            return false;
        }

        public static void FeedComments(MetadataItem item, ITripleSlashCommentParserContext context)
        {
            if (!string.IsNullOrEmpty(item.RawComment))
            {
                item.Summary = TripleSlashCommentParser.GetSummary(item.RawComment, context);
                item.Remarks = TripleSlashCommentParser.GetRemarks(item.RawComment, context);
                item.Exceptions = TripleSlashCommentParser.GetExceptions(item.RawComment, context);
                item.Sees = TripleSlashCommentParser.GetSees(item.RawComment, context);
                item.SeeAlsos = TripleSlashCommentParser.GetSeeAlsos(item.RawComment, context);
                item.Example = TripleSlashCommentParser.GetExample(item.RawComment, context);
            }
        }

        public static string GetId(ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            var assemblySymbol = symbol as IAssemblySymbol;
            if (assemblySymbol != null)
            {
                return assemblySymbol.MetadataName;
            }
            string str = symbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(str))
            {
                Debug.Fail("Cannot get documentation comment id");
                return symbol.MetadataName;
            }

            return str.ToString().Substring(2);
        }

        public static ApiParameter GetParameterDescription(ISymbol symbol, MetadataItem item, string id, bool isReturn, ITripleSlashCommentParserContext context)
        {
            string raw = item.RawComment;

            string comment = isReturn ?
                TripleSlashCommentParser.GetReturns(raw, context) :
                TripleSlashCommentParser.GetParam(raw, symbol.Name, context);
            return new ApiParameter
            {
                Name = isReturn ? null : symbol.Name,
                Type = id,
                Description = comment,
            };
        }

        public static ApiParameter GetTypeParameterDescription(ITypeParameterSymbol symbol, MetadataItem item, ITripleSlashCommentParserContext context)
        {
            string comment = TripleSlashCommentParser.GetTypeParameter(item.RawComment, symbol.Name, context);
            return new ApiParameter
            {
                Name = symbol.Name,
                Description = comment,
            };
        }

        public static SourceDetail GetSourceDetail(ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            var syntaxRef = symbol.DeclaringSyntaxReferences.LastOrDefault();
            if (symbol.IsExtern || syntaxRef == null)
            {
                return new SourceDetail
                {
                    IsExternalPath = true,
                    Path = symbol.ContainingAssembly?.Name,
                };
            }

            var syntaxNode = syntaxRef.GetSyntax();
            Debug.Assert(syntaxNode != null);
            if (syntaxNode != null)
            {
                var source = new SourceDetail
                {
                    StartLine = syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line,
                    Path = syntaxNode.SyntaxTree.FilePath,
                };

                source.Remote = GitUtility.GetGitDetail(source.Path);
                if (source.Remote != null)
                {
                    source.Path = source.Path.FormatPath(UriKind.Relative, source.Remote.LocalWorkingDirectory);
                }
                return source;
            }

            return null;
        }

        public static MemberType GetMemberTypeFromTypeKind(TypeKind typeKind)
        {
            switch (typeKind)
            {
                case TypeKind.Class:
                    return MemberType.Class;
                case TypeKind.Enum:
                    return MemberType.Enum;
                case TypeKind.Interface:
                    return MemberType.Interface;
                case TypeKind.Struct:
                    return MemberType.Struct;
                case TypeKind.Delegate:
                    return MemberType.Delegate;
                default:
                    return MemberType.Default;
            }
        }
    }
}
