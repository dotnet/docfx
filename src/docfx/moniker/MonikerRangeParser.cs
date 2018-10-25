// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        private MonikerRangeParser(EvaluatorWithMonikersVisitor monikersEvaluator)
        {
            _monikersEvaluator = monikersEvaluator;
        }

        public static (List<Error>, MonikerRangeParser) Create(IEnumerable<Moniker> monikers)
        {
            var (errors, evaluatorWithMonikersVisitor) = EvaluatorWithMonikersVisitor.Create(monikers);
            return (errors, new MonikerRangeParser(evaluatorWithMonikersVisitor));
        }

        public (List<Error>, IEnumerable<string>) Parse(string rangeString)
        {
            var errors = new List<Error>();
            IEnumerable<string> monikerNames = null;
            try
            {
                var expression = ExpressionCreator.Create(rangeString);
                monikerNames = expression.Accept(_monikersEvaluator);
            }
            catch (MonikerRangeException ex)
            {
                errors.Add(Errors.InvalidMonikerRange(rangeString, ex.Message));
            }

            return (errors, monikerNames);
        }
    }
}
