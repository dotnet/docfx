// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MonikersProvider
    {
        private readonly List<(Func<string, bool> glob, string monikerRange)> _rules = new List<(Func<string, bool>, string)>();

        public MonikersProvider(Config config)
        {
            foreach (var (key, monikerRange) in config.MonikerRange)
            {
                _rules.Add((GlobUtility.CreateGlobMatcher(key), monikerRange));
            }
            _rules.Reverse();
        }

        public (Error, List<string>) GetFileLevelMonikers(Document file, string fileMonikerRange = null)
        {
            Error error = null;
            string configMonikerRange = null;
            var configMonikers = new List<string>();
            foreach (var (glob, monikerRange) in _rules)
            {
                if (glob(file.FilePath))
                {
                    configMonikerRange = monikerRange;
                    configMonikers = file.Docset.MonikerRangeParser.Parse(monikerRange);
                }
            }

            if (!string.IsNullOrEmpty(fileMonikerRange))
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (configMonikers.Count == 0)
                {
                    error = Errors.MonikerConfigMissing();
                    return (error, configMonikers);
                }

                var fileMonikers = file.Docset.MonikerRangeParser.Parse(fileMonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers, StringComparer.OrdinalIgnoreCase).ToList();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Count == 0)
                {
                    error = Errors.EmptyMonikers($"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is '{string.Join(',', configMonikers)}', while file moniker range '{fileMonikerRange}' is '{string.Join(',', fileMonikers)}'");
                }
                intersection.Sort(file.Docset.MonikerAscendingComparer);
                return (error, intersection);
            }

            configMonikers.Sort(file.Docset.MonikerAscendingComparer);
            return (error, configMonikers);
        }

        public List<string> GetZoneMonikers(Document file, string rangeString, List<string> fileLevelMonikers, List<Error> errors)
        {
            var monikers = new List<string>();

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Count == 0)
            {
                errors.Add(Errors.MonikerConfigMissing());
                return new List<string>();
            }

            var zoneLevelMonikers = file.Docset.MonikerRangeParser.Parse(rangeString);
            monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToList();

            if (monikers.Count == 0)
            {
                errors.Add(Errors.EmptyMonikers($"No intersection between zone and file level monikers. The result of zone level range string '{rangeString}' is '{string.Join(',', zoneLevelMonikers)}', while file level monikers is '{string.Join(',', fileLevelMonikers)}'."));
            }
            monikers.Sort(file.Docset.MonikerAscendingComparer);
            return monikers;
        }
    }
}
