// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly ConcurrentDictionary<string, Lazy<List<string>>> _cache = new ConcurrentDictionary<string, Lazy<List<string>>>();
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public List<string> Parse(string rangeString)
            => string.IsNullOrWhiteSpace(rangeString)
                ? new List<string>()
                : _cache.GetOrAdd(rangeString, new Lazy<List<string>>(() =>
                {
                    List<string> monikerNames = new List<string>();

                    try
                    {
                        var expression = ExpressionCreator.Create(rangeString);
                        monikerNames = expression.Accept(_monikersEvaluator).ToList();
                        monikerNames.Sort();
                    }
                    catch (MonikerRangeException ex)
                    {
                        throw Errors.InvalidMonikerRange(rangeString, ex.Message).ToException();
                    }

                    return monikerNames;
                })).Value;
    }
}
