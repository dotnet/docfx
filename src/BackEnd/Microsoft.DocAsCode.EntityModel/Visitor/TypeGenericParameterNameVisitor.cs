﻿namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;

    internal sealed class TypeGenericParameterNameVisitor : SymbolVisitor<List<string>>
    {
        public static readonly TypeGenericParameterNameVisitor Instance = new TypeGenericParameterNameVisitor();

        private TypeGenericParameterNameVisitor()
        {
        }

        public override List<string> DefaultVisit(ISymbol symbol)
        {
            return null;
        }

        public override List<string> VisitNamedType(INamedTypeSymbol symbol)
        {
            List<string> result = null;
            if (symbol.ContainingType != null)
            {
                result = symbol.ContainingType.Accept(this);
            }
            if (symbol.TypeParameters.Length > 0)
            {
                if (result == null)
                {
                    result = new List<string>();
                }
                for (int i = 0; i < symbol.TypeParameters.Length; i++)
                {
                    result.Add(symbol.TypeParameters[i].Name);
                }
            }
            return result;
        }

        public override List<string> VisitEvent(IEventSymbol symbol)
        {
            return symbol.ContainingType.Accept(this);
        }

        public override List<string> VisitField(IFieldSymbol symbol)
        {
            return symbol.ContainingType.Accept(this);
        }

        public override List<string> VisitMethod(IMethodSymbol symbol)
        {
            return symbol.ContainingType.Accept(this);
        }

        public override List<string> VisitProperty(IPropertySymbol symbol)
        {
            return symbol.ContainingType.Accept(this);
        }

        public override List<string> VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            return symbol.ContainingType.Accept(this);
        }
    }
}
