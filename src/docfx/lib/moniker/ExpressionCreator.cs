// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build;

internal class ExpressionCreator
{
    private static readonly Regex s_orSymbolRegex = new(@"^\s*(?<or>\|\|)", RegexOptions.Compiled);
    private static readonly Regex s_operatorSymbolRegex = new(@"^\s*(?<operator>\=|[\>\<]\=?)", RegexOptions.Compiled);
    private static readonly Regex s_monikerSymbolRegex = new(@"^\s*(?<moniker>[\w\-\.]+)", RegexOptions.Compiled);

    private static readonly Dictionary<string, ComparatorOperatorType> s_operatorMap = new()
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
        var (rangeErrors, expression) = GetMonikerRange(ref rangeString, source);
        errors.AddRange(rangeErrors);
        if (!string.IsNullOrWhiteSpace(rangeString))
        {
            errors.Add(Errors.Versioning.MonikerRangeInvalid(source, $"Parse ends before reaching end of string, unrecognized string: '{rangeString}'."));
        }

        return (errors, expression);
    }

    private static (List<Error>, IExpression?) GetMonikerRange(ref string rangeString, SourceInfo? source)
    {
        var errors = new List<Error>();
        var (firstError, result) = GetComparatorSet(ref rangeString, source);
        errors.AddRange(firstError);
        if (result is null)
        {
            return (errors, result);
        }
        while (Accept(ref rangeString, SymbolType.Or, out _))
        {
            var (compareErrors, comparatorSet) = GetComparatorSet(ref rangeString, source);
            errors.AddRange(compareErrors);
            if (comparatorSet is null)
            {
                break;
            }
            result = new LogicExpression(result, LogicOperatorType.Or, comparatorSet);
        }
        return (errors, result);
    }

    private static (List<Error>, IExpression?) GetComparatorSet(ref string rangeString, SourceInfo? source)
    {
        var errors = new List<Error>();
        IExpression? result = null;
        while (true)
        {
            if (!TryGetComparator(ref rangeString, source, out var comparator, out var compareError))
            {
                errors.AddIfNotNull(compareError);
                break;
            }
            else if (comparator != null)
            {
                result = result != null ? new LogicExpression(result, LogicOperatorType.And, comparator) : comparator;
            }
        }

        if (result is null)
        {
            errors.Add(Errors.Versioning.MonikerRangeInvalid(source, $"Expect a comparator set, but got '{rangeString}'."));
        }
        return (errors, result);
    }

    private static bool TryGetComparator(ref string rangeString, SourceInfo? source, out IExpression? comparator, out Error? error)
    {
        error = null;
        comparator = null;
        var foundOperator = Accept(ref rangeString, SymbolType.Operator, out var @operator);
        if (!foundOperator)
        {
            @operator = "=";
        }
        if (Accept(ref rangeString, SymbolType.Moniker, out var moniker))
        {
            comparator = new ComparatorExpression(s_operatorMap[@operator], moniker);
            return true;
        }
        else if (!foundOperator)
        {
            comparator = null;
            return false;
        }
        else
        {
            error = Errors.Versioning.MonikerRangeInvalid(source, $"Expect a moniker string, but got '{rangeString}'.");
            return false;
        }
    }

    private static bool Accept(ref string rangeString, SymbolType type, out string value)
    {
        value = "";
        if (!string.IsNullOrEmpty(rangeString) && TryMatchSymbol(ref rangeString, type, out value))
        {
            return true;
        }
        return false;
    }

    private static bool TryMatchSymbol(ref string rangeString, SymbolType type, out string value)
    {
        value = "";

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
            rangeString = rangeString[match.Length..];
            return true;
        }

        return false;
    }
}
