// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class LogicExpression : IExpression
{
    public LogicOperatorType OperatorType { get; }

    public IExpression Left { get; }

    public IExpression Right { get; }

    public LogicExpression(IExpression left, LogicOperatorType operatorType, IExpression right)
    {
        Left = left;
        OperatorType = operatorType;
        Right = right;
    }

    public (List<Error>, IEnumerable<Moniker>) Accept(EvaluatorWithMonikersVisitor visitor, SourceInfo<string?> monikerRange)
    {
        return visitor.Visit(this, monikerRange);
    }
}
