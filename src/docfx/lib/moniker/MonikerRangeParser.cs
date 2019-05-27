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
        private readonly ConcurrentDictionary<string, Lazy<IReadOnlyList<string>>> _cache = new ConcurrentDictionary<string, Lazy<IReadOnlyList<string>>>();
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(EvaluatorWithMonikersVisitor monikersEvaluator)
        {
            _monikersEvaluator = monikersEvaluator;
        }

        public IReadOnlyList<string> Parse(SourceInfo<string> rangeString)
            => string.IsNullOrWhiteSpace(rangeString)
                ? Array.Empty<string>()
                : _cache.GetOrAdd(rangeString, new Lazy<IReadOnlyList<string>>(() =>
                {
                    List<string> monikerNames = new List<string>();

                    try
                    {
                        var expression = ExpressionCreator.Create(rangeString);
                        monikerNames = expression.Accept(_monikersEvaluator).ToList();
                        monikerNames.Sort(StringComparer.OrdinalIgnoreCase);
                    }
                    catch (MonikerRangeException ex)
                    {
                        throw Errors.MonikerRangeInvalid(rangeString, ex.Message).ToException();
                    }

                    return monikerNames;
                })).Value;
    }
}
