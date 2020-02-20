// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeParser
    {
        private readonly ConcurrentDictionary<string, string[]> _cache = new ConcurrentDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(EvaluatorWithMonikersVisitor monikersEvaluator)
        {
            _monikersEvaluator = monikersEvaluator;
        }

        public string[] Parse(SourceInfo<string> rangeString)
        {
            if (string.IsNullOrWhiteSpace(rangeString))
            {
                return Array.Empty<string>();
            }

            return _cache.GetOrAdd(rangeString, value =>
            {
                try
                {
                    return ExpressionCreator.Create(value)
                        .Accept(_monikersEvaluator)
                        .Select(x => x.MonikerName)
                        .OrderBy(_ => _, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (MonikerRangeException ex)
                {
                    throw Errors.MonikerRangeInvalid(rangeString, ex).ToException();
                }
            });
        }
    }
}
