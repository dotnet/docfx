// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class ExpressionCreator
    {
        private static readonly Regex s_orSymbolRegex = new Regex(@"^\s*(?<or>\|\|)", RegexOptions.Compiled);
        private static readonly Regex s_operatorSymbolRegex = new Regex(@"^\s*(?<operator>\=|[\>\<]\=?)", RegexOptions.Compiled);
        private static readonly Regex s_monikerSymbolRegex = new Regex(@"^\s*(?<moniker>[\w\-\.]+)", RegexOptions.Compiled);

        private static readonly Dictionary<string, ComparatorOperatorType> s_operatorMap = new Dictionary<string, ComparatorOperatorType>
        {
            { "=", ComparatorOperatorType.EqualTo },
            { ">", ComparatorOperatorType.GreaterThan },
            { "<", ComparatorOperatorType.LessThan },
            { ">=", ComparatorOperatorType.GreaterThanOrEqualTo },
            { "<=", ComparatorOperatorType.LessThanOrEqualTo },
        };

        public static (List<Error>, IExpression?) Create(string rangeString, SourceInfo? source)
        {
            var errors = new List<Error>();
            var (rangeErrors, expression) = GetMonikerRange(rangeString, source);
            errors.AddRange(rangeErrors);
            if (!string.IsNullOrWhiteSpace(rangeString))
            {
                errors.Add(Errors.Versioning.MonikerRangeInvalid(source, $"Parse ends before reaching end of string, unrecognized string: `{rangeString}`"));
            }

            return (errors, expression);
        }

        private static (List<Error>, IExpression?) GetMonikerRange(string rangeString, SourceInfo? source)
        {
            var errors = new List<Error>();
            var (firstErros, result) = GetComparatorSet(rangeString, source);
            errors.AddRange(firstErros);
            if (result is null)
            {
                return (errors, result);
            }
            while (Accept(rangeString, SymbolType.Or, out _))
            {
                var (compareErrors, comparatorSet) = GetComparatorSet(rangeString, source);
                errors.AddRange(compareErrors);
                if (comparatorSet is null)
                {
                    break;
                }
                result = new LogicExpression(result, LogicOperatorType.Or, comparatorSet);
            }
            return (errors, result);
        }

        private static (List<Error>, IExpression?) GetComparatorSet(string rangeString, SourceInfo? source)
        {
            var errors = new List<Error>();
            IExpression? result = null;
            while (TryGetComparator(rangeString, source, out var comparator, out var compareError) && comparator != null)
            {
                errors.AddIfNotNull(compareError);
                if (result != null)
                {
                    result = new LogicExpression(result, LogicOperatorType.And, comparator);
                }
                else
                {
                    result = comparator;
                }
            }
            if (result is null)
            {
                errors.Add(Errors.Versioning.MonikerRangeInvalid(source, $"Expect a comparator set, but got '{rangeString}'"));
            }
            return (errors, result);
        }

        private static bool TryGetComparator(string rangeString, SourceInfo? source, out IExpression? comparator, out Error? error)
        {
            error = null;
            comparator = null;
            var foundOperator = Accept(rangeString, SymbolType.Operator, out var @operator);
            if (!foundOperator)
            {
                @operator = "=";
            }
            if (Accept(rangeString, SymbolType.Moniker, out var moniker))
            {
                comparator = new ComparatorExpression(s_operatorMap[@operator], new SourceInfo<string?>(moniker));
                return true;
            }
            else if (!foundOperator)
            {
                comparator = null;
                return false;
            }
            else
            {
                error = Errors.Versioning.MonikerRangeInvalid(source, $"Expect a moniker string, but got `{rangeString}`");
                return false;
            }
        }

        private static bool Accept(string rangeString, SymbolType type, out string value)
        {
            value = string.Empty;
            if (!string.IsNullOrEmpty(rangeString) && TryMatchSymbol(rangeString, type, out value))
            {
                return true;
            }
            return false;
        }

        private static bool TryMatchSymbol(string rangeString, SymbolType type, out string value)
        {
            value = string.Empty;

            Match match;

            switch (type)
            {
                case SymbolType.Or:
                    match = s_orSymbolRegex.Match(rangeString);
                    break;
                case SymbolType.Moniker:
                    match = s_monikerSymbolRegex.Match(rangeString);
                    break;
                case SymbolType.Operator:
                    match = s_operatorSymbolRegex.Match(rangeString);
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
