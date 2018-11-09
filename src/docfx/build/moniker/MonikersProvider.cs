// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MonikersProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<IEnumerable<string>>> _cache = new ConcurrentDictionary<string, Lazy<IEnumerable<string>>>();
        private readonly List<(Func<string, bool> glob, IEnumerable<string> monikers)> _rules = new List<(Func<string, bool>, IEnumerable<string>)>();

        public MonikersProvider(Config config, MonikerRangeParser monikerRangeParser)
        {
            foreach (var (key, monikerRange) in config.MonikerRange)
            {
                _rules.Add((GlobUtility.CreateGlobMatcher(key), monikerRangeParser.Parse(monikerRange)));
            }
            _rules.Reverse();
        }

        public IEnumerable<string> GetMonikers(Document file)
            => _cache.GetOrAdd(file.FilePath, new Lazy<IEnumerable<string>>(() =>
            {
                // TODO: merge with the monikers from yaml header
                foreach (var (glob, monikers) in _rules)
                {
                    if (glob(file.FilePath))
                    {
                        return monikers;
                    }
                }
                return Array.Empty<string>();
            })).Value;
    }
}
