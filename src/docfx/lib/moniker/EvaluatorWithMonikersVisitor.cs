// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class EvaluatorWithMonikersVisitor
    {
        private readonly Dictionary<string, List<Moniker>> _productMoniker;

        public EvaluatorWithMonikersVisitor(MonikerDefinitionModel monikerDefinition)
        {
            MonikerMap = monikerDefinition.Monikers.ToDictionary(x => x.MonikerName, StringComparer.OrdinalIgnoreCase);
            _productMoniker = monikerDefinition.Monikers.GroupBy(x => x.ProductName).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());
            MonikerOrder = SetMonikerOrder();
        }

        public Dictionary<string, Moniker> MonikerMap { get; }

        public Dictionary<string, (string productName, int orderInProduct)> MonikerOrder { get; }

        public (List<Error>, IEnumerable<Moniker>) Visit(ComparatorExpression expression, SourceInfo<string?> monikerRange)
        {
            var errors = new List<Error>();
            if (!MonikerOrder.TryGetValue(expression.Operand, out var moniker))
            {
                errors.Add(Errors.Versioning.MonikerRangeInvalid(monikerRange, $"Invalid moniker range '{monikerRange}': Moniker '{expression.Operand}' is not defined"));
                return (errors, Array.Empty<Moniker>());
            }

            return expression.Operator switch
            {
                ComparatorOperatorType.EqualTo => (errors, new List<Moniker> { MonikerMap[expression.Operand] }),
                ComparatorOperatorType.GreaterThan => (errors, _productMoniker[moniker.productName].Skip(moniker.orderInProduct + 1)),
                ComparatorOperatorType.GreaterThanOrEqualTo => (errors, _productMoniker[moniker.productName].Skip(moniker.orderInProduct)),
                ComparatorOperatorType.LessThan => (errors, _productMoniker[moniker.productName].Take(moniker.orderInProduct)),
                ComparatorOperatorType.LessThanOrEqualTo => (errors, _productMoniker[moniker.productName].Take(moniker.orderInProduct + 1)),
                _ => (errors, Array.Empty<Moniker>()),
            };
        }

        public (List<Error>, IEnumerable<Moniker>) Visit(LogicExpression expression, SourceInfo<string?> monikerRange)
        {
            var errors = new List<Error>();
            var (leftError, left) = expression.Left.Accept(this, monikerRange);
            errors.AddRange(leftError);
            var (rightError, right) = expression.Right.Accept(this, monikerRange);
            errors.AddRange(rightError);

            return expression.OperatorType switch
            {
                LogicOperatorType.And => (errors, left.Intersect(right)),
                LogicOperatorType.Or => (errors, left.Union(right)),
                _ => (errors, Array.Empty<Moniker>()),
            };
        }

        private Dictionary<string, (string, int)> SetMonikerOrder()
        {
            var result = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
            foreach (var (productName, productMonikerList) in _productMoniker)
            {
                for (var i = 0; i < productMonikerList.Count; i++)
                {
                    result[productMonikerList[i].MonikerName] = (productName, i);
                }
            }

            return result;
        }
    }
}
