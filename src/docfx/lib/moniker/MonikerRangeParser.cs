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
        private readonly ConcurrentDictionary<string, Lazy<IReadOnlyList<Moniker>>> _cache = new ConcurrentDictionary<string, Lazy<IReadOnlyList<Moniker>>>();
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(EvaluatorWithMonikersVisitor monikersEvaluator)
        {
            _monikersEvaluator = monikersEvaluator;
        }

        public IReadOnlyList<string> Parse(SourceInfo<string> rangeString)
        {
            var monikerNames = ParseWithInfo(rangeString).Select(x => x.MonikerName).ToList();
            monikerNames.Sort(StringComparer.OrdinalIgnoreCase);
            return monikerNames;
        }

        public IReadOnlyList<Moniker> ParseWithInfo(SourceInfo<string> rangeString)
            => string.IsNullOrWhiteSpace(rangeString)
                ? Array.Empty<Moniker>()
                : _cache.GetOrAdd(rangeString, new Lazy<IReadOnlyList<Moniker>>(() =>
                {
                    List<Moniker> monikers = new List<Moniker>();

                    try
                    {
                        var expression = ExpressionCreator.Create(rangeString);
                        monikers = expression.Accept(_monikersEvaluator).ToList();
                    }
                    catch (MonikerRangeException ex)
                    {
                        throw Errors.MonikerRangeInvalid(rangeString, ex.Message).ToException();
                    }

                    return monikers;
                })).Value;
    }
}
