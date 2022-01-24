// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class MonikerRangeParser
{
    private readonly ConcurrentDictionary<string, MonikerList> _cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly EvaluatorWithMonikersVisitor _monikersEvaluator;

    public MonikerRangeParser(MonikerDefinitionModel monikerDefinition)
    {
        _monikersEvaluator = new(monikerDefinition);
    }

    public MonikerList Validate(ErrorBuilder errors, SourceInfo<string>[] monikers)
    {
        var result = new List<string>();
        foreach (var moniker in monikers)
        {
            var key = moniker.Value;
            if (key != null)
            {
                if (!_monikersEvaluator.MonikerMap.ContainsKey(key))
                {
                    errors.Add(Errors.Versioning.MonikerRangeInvalid(moniker, $"Moniker '{key}' is not defined."));
                }
                else
                {
                    result.Add(key.ToLowerInvariant());
                }
            }
        }
        return new(result);
    }

    public MonikerList Parse(ErrorBuilder errors, SourceInfo<string?> rangeString)
    {
        var key = rangeString.Value;
        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        return _cache.GetOrAdd(key, value =>
        {
            var (createErrors, result) = ExpressionCreator.Create(value, rangeString.Source);
            errors.AddRange(createErrors);
            if (result is null)
            {
                return default;
            }

            var (evaluateErrors, monikers) = result.Accept(_monikersEvaluator, rangeString);
            errors.AddRange(evaluateErrors);

            return new(monikers.Select(x => x.MonikerName));
        });
    }
}
