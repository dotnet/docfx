// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(IEnumerable<Moniker> monikers)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikers);
        }

        public IEnumerable<string> Parse(string rangeString)
        {
            IEnumerable<string> monikerNames = null;
            try
            {
                var expression = ExpressionCreator.Create(rangeString);
                monikerNames = expression.Accept(_monikersEvaluator);
            }
            catch (MonikerRangeException ex)
            {
                throw Errors.InvalidMonikerRange(rangeString, ex.Message).ToException();
            }

            return monikerNames;
        }
    }
}
