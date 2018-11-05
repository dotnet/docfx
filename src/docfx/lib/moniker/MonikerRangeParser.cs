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
            if (monikerDefinition == null)
            {
                _monikersEvaluator = null;
            }
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public static (List<Error>, MonikerRangeParser) Create(Context context, string monikerDefinitionFile)
        {
            if (string.IsNullOrEmpty(monikerDefinitionFile))
            {
                return (new List<Error>(), new MonikerRangeParser(null));
            }
            var (errors, monikerDefinition) = JsonUtility.Deserialize<MonikerDefinitionModel>(File.ReadAllText(monikerDefinitionFile));
            return (errors, new MonikerRangeParser(monikerDefinition));
        }

        public IEnumerable<string> Parse(string rangeString)
        {
            if (_monikersEvaluator == null)
            {
                throw Errors.MonikerDefinitionNotFound(rangeString).ToException();
            }
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
