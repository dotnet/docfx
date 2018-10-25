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

        private EvaluatorWithMonikersVisitor(Dictionary<string, MonikerProductInfo> monikerProductInfoDictionary)
        {
            _monikerProductInfoDictionary = monikerProductInfoDictionary;
        }

        public static (List<Error>, EvaluatorWithMonikersVisitor) Create(IEnumerable<Moniker> monikers)
        {
            Debug.Assert(monikers != null);

            var (errors, monikerProductInfoDictionary) = InitializeMonikers(monikers);

            return (errors, new EvaluatorWithMonikersVisitor(monikerProductInfoDictionary));
        }

        public IEnumerable<string> Visit(ComparatorExpression expression)
        {
            Debug.Assert(expression.Operand != null);
            if (!_monikerProductInfoDictionary.TryGetValue(expression.Operand, out var monikerProductInfo))
            {
                throw new MonikerRangeException($"Moniker `{expression.Operand}` is not found in available monikers list");
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
                    return null;
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
                    return null;
            }
        }

        private static (List<Error>, Dictionary<string, MonikerProductInfo>) InitializeMonikers(IEnumerable<Moniker> monikers)
        {
            var errors = new List<Error>();
            var monikerName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var productNameDictionary = new Dictionary<string, List<Moniker>>(StringComparer.OrdinalIgnoreCase);

            foreach (var moniker in monikers)
            {
                if (monikerName.Contains(moniker.MonikerName))
                {
                    errors.Add(Errors.MonikerNameConflict(moniker.MonikerName));
                }
                else
                {
                    monikerName.Add(moniker.MonikerName);

                    List<Moniker> list;
                    if (productNameDictionary.TryGetValue(moniker.ProductName, out list))
                    {
                        list.Add(moniker);
                    }
                    else
                    {
                        productNameDictionary[moniker.ProductName] = new List<Moniker> { moniker };
                    }
                }
            }

            var result = new Dictionary<string, MonikerProductInfo>();
            foreach (var productMonikerList in productNameDictionary.Values)
            {
                var orderedMonikerNameList = productMonikerList
                    .OrderBy(m => m.Order)
                    .Select(m => m.MonikerName)
                    .ToList();
                for (var i = 0; i < orderedMonikerNameList.Count(); i++)
                {
                    result[orderedMonikerNameList[i]] = new MonikerProductInfo
                    {
                        Index = i,
                        OrderedProductMonikerNames = orderedMonikerNameList,
                    };
                }
            }

            return (errors, result);
        }

        private class MonikerProductInfo
        {
            public int Index { get; set; }

            public List<string> OrderedProductMonikerNames { get; set; }
        }
    }
}
