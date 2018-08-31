// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public abstract class ReferenceItemVisitor : SymbolVisitor
    {
        public static readonly SymbolDisplayFormat ShortFormat = SimpleYamlModelGenerator.ShortFormat;
        public static readonly SymbolDisplayFormat QualifiedFormat = SimpleYamlModelGenerator.QualifiedFormat;
        public static readonly SymbolDisplayFormat ShortFormatWithoutGenericeParameter = ShortFormat
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None);
        public static readonly SymbolDisplayFormat QualifiedFormatWithoutGenericeParameter = QualifiedFormat
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None);

        protected ReferenceItemVisitor(ReferenceItem referenceItem)
        {
            ReferenceItem = referenceItem;
        }

        protected ReferenceItem ReferenceItem { get; private set; }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.IsTupleType)
            {
                symbol = symbol.TupleUnderlyingType;
            }
            if (symbol.IsGenericType)
            {
                if (symbol.IsUnboundGenericType)
                {
                    AddLinkItems(symbol, true);
                }
                else
                {
                    AddLinkItems(symbol.OriginalDefinition, false);
                    AddBeginGenericParameter();
                    for (int i = 0; i < symbol.TypeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            AddGenericParameterSeparator();
                        }
                        symbol.TypeArguments[i].Accept(this);
                    }
                    AddEndGenericParameter();
                }
            }
            else
            {
                AddLinkItems(symbol, true);
            }
        }

        protected abstract void AddLinkItems(INamedTypeSymbol symbol, bool withGenericeParameter);

        protected abstract void AddBeginGenericParameter();

        protected abstract void AddGenericParameterSeparator();

        protected abstract void AddEndGenericParameter();
    }

    public class CSReferenceItemVisitor
        : ReferenceItemVisitor
    {
        private readonly bool _asOverload;

        public CSReferenceItemVisitor(ReferenceItem referenceItem, bool asOverload) : base(referenceItem)
        {
            _asOverload = asOverload;
            if (!referenceItem.Parts.ContainsKey(SyntaxLanguage.CSharp))
            {
                referenceItem.Parts.Add(SyntaxLanguage.CSharp, new List<LinkItem>());
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified).GetName(symbol),
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
        }

        public override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = symbol.Name,
                DisplayNamesWithType = symbol.Name,
                DisplayQualifiedNames = symbol.Name,
            });
        }

        public override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            symbol.ElementType.Accept(this);
            if (symbol.Rank == 1)
            {
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = "[]",
                    DisplayNamesWithType = "[]",
                    DisplayQualifiedNames = "[]",
                });
            }
            else
            {
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = "[" + new string(',', symbol.Rank - 1) + "]",
                    DisplayNamesWithType = "[" + new string(',', symbol.Rank - 1) + "]",
                    DisplayQualifiedNames = "[" + new string(',', symbol.Rank - 1) + "]",
                });
            }
        }

        public override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            symbol.PointedAtType.Accept(this);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = "*",
                DisplayNamesWithType = "*",
                DisplayQualifiedNames = "*",
            });
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            var id = _asOverload ? VisitorHelper.GetOverloadId(symbol.OriginalDefinition) : VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType | (_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter)).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified | (_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter)).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
            if (_asOverload)
            {
                return;
            }
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = "(",
                DisplayNamesWithType = "(",
                DisplayQualifiedNames = "(",
            });
            for (int i = 0; i < symbol.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                    {
                        DisplayName = ", ",
                        DisplayNamesWithType = ", ",
                        DisplayQualifiedNames = ", ",
                    });
                }
                symbol.Parameters[i].Type.Accept(this);
            }
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = ")",
                DisplayNamesWithType = ")",
                DisplayQualifiedNames = ")",
            });
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            var id = _asOverload ? VisitorHelper.GetOverloadId(symbol.OriginalDefinition) : VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
            if (symbol.Parameters.Length > 0 && !_asOverload)
            {
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = "[",
                    DisplayNamesWithType = "[",
                    DisplayQualifiedNames = "[",
                });
                for (int i = 0; i < symbol.Parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                        {
                            DisplayName = ", ",
                            DisplayNamesWithType = ", ",
                            DisplayQualifiedNames = ", ",
                        });
                    }
                    symbol.Parameters[i].Type.Accept(this);
                }
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = "]",
                    DisplayNamesWithType = "]",
                    DisplayQualifiedNames = "]",
                });
            }
        }

        public override void VisitEvent(IEventSymbol symbol)
        {
            var id = VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
        }

        public override void VisitField(IFieldSymbol symbol)
        {
            var id = VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
        }

        public override void VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            var id = VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified).GetName(symbol),
                Name = id,
            });
        }

        protected override void AddBeginGenericParameter()
        {
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = "<",
                DisplayNamesWithType = "<",
                DisplayQualifiedNames = "<",
            });
        }

        protected override void AddEndGenericParameter()
        {
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = ">",
                DisplayNamesWithType = ">",
                DisplayQualifiedNames = ">",
            });
        }

        protected override void AddGenericParameterSeparator()
        {
            ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
            {
                DisplayName = ", ",
                DisplayNamesWithType = ", ",
                DisplayQualifiedNames = ", ",
            });
        }

        protected override void AddLinkItems(INamedTypeSymbol symbol, bool withGenericeParameter)
        {
            var id = VisitorHelper.GetId(symbol);
            if (withGenericeParameter)
            {
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = NameVisitorCreator.GetCSharp(NameOptions.WithGenericParameter).GetName(symbol),
                    DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithGenericParameter).GetName(symbol),
                    DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithGenericParameter).GetName(symbol),
                    Name = id,
                    IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
                });
            }
            else
            {
                ReferenceItem.Parts[SyntaxLanguage.CSharp].Add(new LinkItem
                {
                    DisplayName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(symbol),
                    DisplayNamesWithType = NameVisitorCreator.GetCSharp(NameOptions.WithType).GetName(symbol),
                    DisplayQualifiedNames = NameVisitorCreator.GetCSharp(NameOptions.Qualified).GetName(symbol),
                    Name = id,
                    IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
                });
            }
        }
    }

    public class VBReferenceItemVisitor
        : ReferenceItemVisitor
    {
        private readonly bool _asOverload;

        public VBReferenceItemVisitor(ReferenceItem referenceItem, bool asOverload) : base(referenceItem)
        {
            _asOverload = asOverload;
            if (!referenceItem.Parts.ContainsKey(SyntaxLanguage.VB))
            {
                referenceItem.Parts.Add(SyntaxLanguage.VB, new List<LinkItem>());
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetVB(NameOptions.None).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified).GetName(symbol),
            });
        }

        public override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = symbol.Name,
                DisplayNamesWithType = symbol.Name,
                DisplayQualifiedNames = symbol.Name,
            });
        }

        public override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            symbol.ElementType.Accept(this);
            if (symbol.Rank == 1)
            {
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = "()",
                    DisplayNamesWithType = "()",
                    DisplayQualifiedNames = "()",
                });
            }
            else
            {
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = "(" + new string(',', symbol.Rank - 1) + ")",
                    DisplayNamesWithType = "(" + new string(',', symbol.Rank - 1) + ")",
                    DisplayQualifiedNames = "(" + new string(',', symbol.Rank - 1) + ")",
                });
            }
        }

        public override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            symbol.PointedAtType.Accept(this);
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = "*",
                DisplayNamesWithType = "*",
                DisplayQualifiedNames = "*",
            });
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            var id = _asOverload ? VisitorHelper.GetOverloadId(symbol.OriginalDefinition) : VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetVB(_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType | (_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter)).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified | (_asOverload ? NameOptions.WithTypeGenericParameter : NameOptions.WithGenericParameter)).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
            if (_asOverload)
            {
                return;
            }
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = "(",
                DisplayNamesWithType = "(",
                DisplayQualifiedNames = "(",
            });
            for (int i = 0; i < symbol.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                    {
                        DisplayName = ", ",
                        DisplayNamesWithType = ", ",
                        DisplayQualifiedNames = ", ",
                    });
                }
                symbol.Parameters[i].Type.Accept(this);
            }
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = ")",
                DisplayNamesWithType = ")",
                DisplayQualifiedNames = ")",
            });
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            var id = _asOverload ? VisitorHelper.GetOverloadId(symbol.OriginalDefinition) : VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetVB(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
            if (symbol.Parameters.Length > 0 && !_asOverload)
            {
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = "(",
                    DisplayNamesWithType = "(",
                    DisplayQualifiedNames = "(",
                });
                for (int i = 0; i < symbol.Parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                        {
                            DisplayName = ", ",
                            DisplayNamesWithType = ", ",
                            DisplayQualifiedNames = ", ",
                        });
                    }
                    symbol.Parameters[i].Type.Accept(this);
                }
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = ")",
                    DisplayNamesWithType = ")",
                    DisplayQualifiedNames = ")",
                });
            }
        }

        public override void VisitEvent(IEventSymbol symbol)
        {
            var id = VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetVB(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
        }

        public override void VisitField(IFieldSymbol symbol)
        {
            var id = VisitorHelper.GetId(symbol.OriginalDefinition);
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = NameVisitorCreator.GetVB(NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType | NameOptions.WithTypeGenericParameter).GetName(symbol),
                DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified | NameOptions.WithTypeGenericParameter).GetName(symbol),
                Name = id,
                IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
            });
        }

        protected override void AddBeginGenericParameter()
        {
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = "(Of ",
                DisplayNamesWithType = "(Of ",
                DisplayQualifiedNames = "(Of ",
            });
        }

        protected override void AddEndGenericParameter()
        {
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = ")",
                DisplayNamesWithType = ")",
                DisplayQualifiedNames = ")",
            });
        }

        protected override void AddGenericParameterSeparator()
        {
            ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
            {
                DisplayName = ", ",
                DisplayNamesWithType = ", ",
                DisplayQualifiedNames = ", ",
            });
        }

        protected override void AddLinkItems(INamedTypeSymbol symbol, bool withGenericeParameter)
        {
            var id = VisitorHelper.GetId(symbol);
            if (withGenericeParameter)
            {
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = NameVisitorCreator.GetVB(NameOptions.WithGenericParameter).GetName(symbol),
                    DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType | NameOptions.WithGenericParameter).GetName(symbol),
                    DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified | NameOptions.WithGenericParameter).GetName(symbol),
                    Name = id,
                    IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
                });
            }
            else
            {
                ReferenceItem.Parts[SyntaxLanguage.VB].Add(new LinkItem
                {
                    DisplayName = NameVisitorCreator.GetVB(NameOptions.None).GetName(symbol),
                    DisplayNamesWithType = NameVisitorCreator.GetVB(NameOptions.WithType).GetName(symbol),
                    DisplayQualifiedNames = NameVisitorCreator.GetVB(NameOptions.Qualified).GetName(symbol),
                    Name = id,
                    IsExternalPath = symbol.IsExtern || symbol.DeclaringSyntaxReferences.Length == 0,
                });
            }
        }
    }
}
