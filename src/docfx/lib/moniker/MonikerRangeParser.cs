// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public static (List<Error>, MonikerRangeParser) Create(string monikerDefinitionFile)
        {
            var errors = new List<Error>();
            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(monikerDefinitionFile))
            {
                (errors, monikerDefinition) = JsonUtility.Deserialize<MonikerDefinitionModel>(File.ReadAllText(monikerDefinitionFile));
            }
            return (errors, new MonikerRangeParser(monikerDefinition));
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
