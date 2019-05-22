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
        private readonly Dictionary<string, MonikerProductInfo> _monikerProductInfoDictionary;

        public EvaluatorWithMonikersVisitor(MonikerDefinitionModel monikerDefinition)
        {
            Debug.Assert(monikerDefinition != null);

            _monikerProductInfoDictionary = InitializeMonikers(monikerDefinition.Monikers);
        }

        public List<string> GetSortedMonikerNameList()
        {
            return _monikerProductInfoDictionary.Keys.ToList();
        }

        public IEnumerable<string> Visit(ComparatorExpression expression)
        {
            Debug.Assert(expression.Operand != null);
            if (!_monikerProductInfoDictionary.TryGetValue(expression.Operand, out var monikerProductInfo))
            {
                throw new MonikerRangeException($"Moniker `{expression.Operand}` is not defined");
            }

            switch (expression.Operator)
            {
                case ComparatorOperatorType.EqualTo:
                    return new List<string> { expression.Operand };
                case ComparatorOperatorType.GreaterThan:
                    return monikerProductInfo.OrderedProductMonikerNames.Skip(monikerProductInfo.Index + 1);
                case ComparatorOperatorType.GreaterThanOrEqualTo:
                    return monikerProductInfo.OrderedProductMonikerNames.Skip(monikerProductInfo.Index);
                case ComparatorOperatorType.LessThan:
                    return monikerProductInfo.OrderedProductMonikerNames.Take(monikerProductInfo.Index);
                case ComparatorOperatorType.LessThanOrEqualTo:
                    return monikerProductInfo.OrderedProductMonikerNames.Take(monikerProductInfo.Index + 1);
                default:
                    return Array.Empty<string>();
            }
        }

        public IEnumerable<string> Visit(LogicExpression expression)
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
                    return Array.Empty<string>();
            }
        }

        private static Dictionary<string, MonikerProductInfo> InitializeMonikers(IEnumerable<Moniker> monikers)
        {
            var monikerNameList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var productNameDictionary = new SortedDictionary<string, List<Moniker>>(StringComparer.OrdinalIgnoreCase);

            foreach (var moniker in monikers)
            {
                if (!monikerNameList.Add(moniker.MonikerName))
                {
                    throw Errors.MonikerNameConflict(moniker.MonikerName).ToException();
                }

                if (productNameDictionary.TryGetValue(moniker.ProductName, out List<Moniker> list))
                {
                    list.Add(moniker);
                }
                else
                {
                    productNameDictionary[moniker.ProductName] = new List<Moniker> { moniker };
                }
            }

            var result = new Dictionary<string, MonikerProductInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var productMonikerList in productNameDictionary.Values)
            {
                var sortedMonikerNameList = productMonikerList.OrderBy(moniker => moniker.Order)
                                                              .Select(moniker => moniker.MonikerName)
                                                              .ToList();
                for (var i = 0; i < sortedMonikerNameList.Count(); i++)
                {
                    result[sortedMonikerNameList[i]] = new MonikerProductInfo
                    {
                        Index = i,
                        OrderedProductMonikerNames = sortedMonikerNameList,
                    };
                }
            }

            return result;
        }

        private class MonikerProductInfo
        {
            public int Index { get; set; }

            public List<string> OrderedProductMonikerNames { get; set; }
        }
    }
}
