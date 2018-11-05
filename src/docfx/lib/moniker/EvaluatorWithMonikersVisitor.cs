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

        private static Dictionary<string, MonikerProductInfo> InitializeMonikers(IEnumerable<MonikerSpec> monikers)
        {
            var monikerName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var productNameDictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var monikerSpec in monikers)
            {
                if (monikerName.Contains(monikerSpec.Moniker))
                {
                    throw Errors.MonikerNameConflict(monikerSpec.Moniker).ToException();
                }
                else
                {
                    monikerName.Add(monikerSpec.Moniker);

                    List<string> list;
                    if (productNameDictionary.TryGetValue(monikerSpec.Product, out list))
                    {
                        list.Add(monikerSpec.Moniker);
                    }
                    else
                    {
                        productNameDictionary[monikerSpec.Product] = new List<string> { monikerSpec.Moniker };
                    }
                }
            }

            var result = new Dictionary<string, MonikerProductInfo>();
            foreach (var productMonikerList in productNameDictionary.Values)
            {
                for (var i = 0; i < productMonikerList.Count(); i++)
                {
                    result[productMonikerList[i]] = new MonikerProductInfo
                    {
                        Index = i,
                        OrderedProductMonikerNames = productMonikerList,
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
