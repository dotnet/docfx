// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class ExpressionCreator
    {
        private static readonly Regex OrSymbolRegex = new Regex(@"^\s*(?<or>\|\|)", RegexOptions.Compiled);
        private static readonly Regex OperatorSymbolRegex = new Regex(@"^\s*(?<operator>\=|[\>\<]\=?)", RegexOptions.Compiled);
        private static readonly Regex MonikerSymbolRegex = new Regex(@"^\s*(?<moniker>[\w\-\.]+)", RegexOptions.Compiled);

        private static readonly Dictionary<string, ComparatorOperatorType> OperatorMap = new Dictionary<string, ComparatorOperatorType>
        {
            { "=", ComparatorOperatorType.EqualTo },
            { ">", ComparatorOperatorType.GreaterThan },
            { "<", ComparatorOperatorType.LessThan },
            { ">=", ComparatorOperatorType.GreaterThanOrEqualTo },
            { "<=", ComparatorOperatorType.LessThanOrEqualTo },
        };

        public static IExpression Create(string rangeString)
        {
            var expression = MonikerRange();
            if (!Eos())
            {
                throw new MonikerRangeException($"Parse ends before reaching end of string, unrecognized string: `{rangeString}`");
            }

            return expression;

            IExpression MonikerRange()
            {
                var result = ComparatorSet();
                while (Accept(SymbolType.Or, out _))
                {
                    result = new LogicExpression
                    {
                        Left = result,
                        OperatorType = LogicOperatorType.Or,
                        Right = ComparatorSet(),
                    };
                }
                return result;
            }

            IExpression ComparatorSet()
            {
                IExpression result = null;
                while (TryGetComparator(out var comparator))
                {
                    if (result != null)
                    {
                        result = new LogicExpression
                        {
                            Left = result,
                            OperatorType = LogicOperatorType.And,
                            Right = comparator,
                        };
                    }
                    else
                    {
                        result = comparator;
                    }
                }
                return result;
            }

            bool TryGetComparator(out IExpression comparator)
            {
                string @operator;
                var foundOperator = Accept(SymbolType.Operator, out @operator);
                if (!foundOperator)
                {
                    @operator = "=";
                }
                if (Accept(SymbolType.Moniker, out string moniker))
                {
                    comparator = new ComparatorExpression(OperatorMap[@operator], moniker);
                    return true;
                }
                else if (!foundOperator)
                {
                    comparator = null;
                    return false;
                }
                else
                {
                    throw new MonikerRangeException($"Expect a moniker string, but got `{rangeString}`");
                }
            }

            bool Accept(SymbolType type, out string value)
            {
                value = string.Empty;
                if (!string.IsNullOrEmpty(rangeString) && TryMatchSymbol(type, out value))
                {
                    return true;
                }
                return false;
            }

            bool Eos()
            {
                return string.IsNullOrWhiteSpace(rangeString);
            }

            bool TryMatchSymbol(SymbolType type, out string value)
            {
                value = string.Empty;

                Match match;

                switch (type)
                {
                    case SymbolType.Or:
                        match = OrSymbolRegex.Match(rangeString);
                        break;
                    case SymbolType.Moniker:
                        match = MonikerSymbolRegex.Match(rangeString);
                        break;
                    case SymbolType.Operator:
                        match = OperatorSymbolRegex.Match(rangeString);
                        break;
                    default:
                        return false;
                }
                if (match.Length > 0)
                {
                    value = match.Groups[1].Value;
                    rangeString = rangeString.Substring(match.Length);
                    return true;
                }

                return false;
            }
        }
    }
}
