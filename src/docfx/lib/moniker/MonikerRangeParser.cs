// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public List<string> Parse(string rangeString)
        {
            List<string> monikerNames = null;
            try
            {
                var expression = ExpressionCreator.Create(rangeString);
                monikerNames = expression.Accept(_monikersEvaluator).ToList();
                monikerNames.Sort(StringComparer.Ordinal);
            }
            catch (MonikerRangeException ex)
            {
                throw Errors.InvalidMonikerRange(rangeString, ex.Message).ToException();
            }

            return monikerNames;
        }
    }
}
