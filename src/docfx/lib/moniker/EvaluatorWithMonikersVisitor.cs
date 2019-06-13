// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class EvaluatorWithMonikersVisitor
    {
        private readonly Dictionary<string, List<Moniker>> _productMoniker;
        private readonly Dictionary<string, Moniker> _monikerMap;

        public EvaluatorWithMonikersVisitor(MonikerDefinitionModel monikerDefinition)
        {
            Debug.Assert(monikerDefinition != null);

            _monikerMap = monikerDefinition.Monikers.ToDictionary(x => x.MonikerName);
            _productMoniker = monikerDefinition.Monikers.GroupBy(x => x.ProductName).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());
            MonikerOrder = SetMonikerOrder();
        }

        public Dictionary<string, (string productName, int orderInProduct)> MonikerOrder { get; }

        public IEnumerable<Moniker> Visit(ComparatorExpression expression)
        {
            Debug.Assert(expression.Operand != null);
            if (!MonikerOrder.TryGetValue(expression.Operand, out var moniker))
            {
                throw new MonikerRangeException($"Moniker `{expression.Operand}` is not defined");
            }

            switch (expression.Operator)
            {
                case ComparatorOperatorType.EqualTo:
                    return new List<Moniker> { _monikerMap[expression.Operand] };
                case ComparatorOperatorType.GreaterThan:
                    return _productMoniker[moniker.productName].Skip(moniker.orderInProduct + 1);
                case ComparatorOperatorType.GreaterThanOrEqualTo:
                    return _productMoniker[moniker.productName].Skip(moniker.orderInProduct);
                case ComparatorOperatorType.LessThan:
                    return _productMoniker[moniker.productName].Take(moniker.orderInProduct);
                case ComparatorOperatorType.LessThanOrEqualTo:
                    return _productMoniker[moniker.productName].Take(moniker.orderInProduct + 1);
                default:
                    return Array.Empty<Moniker>();
            }
        }

        public IEnumerable<Moniker> Visit(LogicExpression expression)
        {
            Debug.Assert(expression.Left != null);
            Debug.Assert(expression.Right != null);

            var left = expression.Left.Accept(this);
            var right = expression.Right.Accept(this);

            switch (expression.OperatorType)
            {
                case LogicOperatorType.And:
                    return left.Intersect(right);
                case LogicOperatorType.Or:
                    return left.Union(right);
                default:
                    return Array.Empty<Moniker>();
            }
        }

        private Dictionary<string, (string, int)> SetMonikerOrder()
        {
            var result = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
            foreach (var (productName, productMonikerList) in _productMoniker)
            {
                for (var i = 0; i < productMonikerList.Count(); i++)
                {
                    result[productMonikerList[i].MonikerName] = (productName, i);
                }
            }

            return result;
        }
    }
}
