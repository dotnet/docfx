// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
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

        public IEnumerable<Moniker> Accept(EvaluatorWithMonikersVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
