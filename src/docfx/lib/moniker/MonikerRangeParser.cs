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
        private readonly ConcurrentDictionary<string, (List<Error>, string[])> _cache = new ConcurrentDictionary<string, (List<Error>, string[])>(StringComparer.OrdinalIgnoreCase);
        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public (List<Error>, string[]) Validate(SourceInfo<string?>[] monikers)
        {
            var errors = new List<Error>();
            var result = new List<string>();
            foreach (var moniker in monikers)
            {
                var key = moniker.Value;
                if (key != null)
                {
                    if (!_monikersEvaluator.MonikerMap.ContainsKey(key))
                    {
                        errors.Add(Errors.Versioning.MonikerRangeInvalid(moniker, $"Invalid monikers: Moniker '{key}' is not defined"));
                    }
                    else
                    {
                        result.Add(key.ToLowerInvariant());
                    }
                }
            }
            return (errors, result.ToArray());
        }

        public (List<Error>, string[]) Parse(SourceInfo<string?> rangeString)
        {
            var key = rangeString.Value;
            if (string.IsNullOrWhiteSpace(key))
            {
                return (new List<Error>(), Array.Empty<string>());
            }

            return _cache.GetOrAdd(key, value =>
            {
                var (errors, result) = ExpressionCreator.Create(value, rangeString.Source);
                if (result is null)
                {
                    return (errors, Array.Empty<string>());
                }
                var (evaluateErrors, monikers) = result.Accept(_monikersEvaluator, rangeString);
                errors.AddRange(evaluateErrors);
                return (errors, monikers
                    .Select(x => x.MonikerName.ToLowerInvariant())
                    .OrderBy(_ => _, StringComparer.Ordinal)
                    .ToArray());
            });
        }
    }
}
