// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class ComparatorExpression : IExpression
    {
        public ComparatorOperatorType Operator { get; set; }

        public string Operand { get; set; }

        public ComparatorExpression(ComparatorOperatorType @operator, string operand)
        {
            Operator = @operator;
            Operand = operand;
        }

        public IEnumerable<Moniker> Accept(EvaluatorWithMonikersVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
