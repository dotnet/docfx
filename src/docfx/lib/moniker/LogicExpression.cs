// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class LogicExpression : IExpression
    {
        public LogicOperatorType OperatorType { get; set; }

        public IExpression Left { get; set; }

        public IExpression Right { get; set; }

        public IEnumerable<string> Accept(EvaluatorWithMonikersVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
