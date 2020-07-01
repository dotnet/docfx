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
        private readonly ConcurrentDictionary<string, (List<Error>, MonikerList)> _cache =
            new ConcurrentDictionary<string, (List<Error>, MonikerList)>(StringComparer.OrdinalIgnoreCase);

        private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

        public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
        {
            _monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
        }

        public (List<Error>, MonikerList) Validate(SourceInfo<string?>[] monikers)
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
                        errors.Add(Errors.Versioning.MonikerRangeInvalid(moniker, $"Invalid monikers: Moniker '{key}' is not defined."));
                    }
                    else
                    {
                        result.Add(key.ToLowerInvariant());
                    }
                }
            }
            return (errors, new MonikerList(result));
        }

        public (List<Error>, MonikerList) Parse(SourceInfo<string?> rangeString)
        {
            var key = rangeString.Value;
            if (string.IsNullOrWhiteSpace(key))
            {
                return (new List<Error>(), default);
            }

            return _cache.GetOrAdd(key, value =>
            {
                var (errors, result) = ExpressionCreator.Create(value, rangeString.Source);
                if (result is null)
                {
                    return (errors, default);
                }

                var (evaluateErrors, monikers) = result.Accept(_monikersEvaluator, rangeString);
                errors.AddRange(evaluateErrors);

                return (errors, new MonikerList(monikers.Select(x => x.MonikerName)));
            });
        }
    }
}
