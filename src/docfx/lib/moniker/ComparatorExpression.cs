// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ComparatorExpression : IExpression
{
    public ComparatorOperatorType Operator { get; set; }

    public string Operand { get; set; }

    public ComparatorExpression(ComparatorOperatorType @operator, string operand)
    {
        Operator = @operator;
        Operand = operand;
    }

    public (List<Error>, IEnumerable<Moniker>) Accept(EvaluatorWithMonikersVisitor visitor, SourceInfo<string?> monikerRange)
    {
        var errors = new List<Error>();
        var (error, monikers) = visitor.Visit(this, monikerRange);
        errors.AddIfNotNull(error);
        return (errors, monikers);
    }
}
