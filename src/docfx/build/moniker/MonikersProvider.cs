// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MonikersProvider
    {
        private readonly List<(Func<string, bool> glob, string monikerRange)> _rules = new List<(Func<string, bool>, string)>();
        private readonly MonikerRangeParser _rangeParser;
        private readonly Lazy<MonikerComparer> _monikerAscendingComparer;
        private readonly Lazy<MonikerComparer> _monikerDescendingComparer;

        public MonikerComparer AscendingComparer => _monikerAscendingComparer.Value;

        public MonikerComparer DescendingComparer => _monikerDescendingComparer.Value;

        public MonikersProvider(Docset docset, Config config)
        {
            foreach (var (key, monikerRange) in config.MonikerRange)
            {
                _rules.Add((GlobUtility.CreateGlobMatcher(key), monikerRange));
            }
            _rules.Reverse();

            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(config.MonikerDefinition))
            {
                var path = docset.GetFileRestorePath(config.MonikerDefinition);
                monikerDefinition = JsonUtility.Deserialize<MonikerDefinitionModel>(File.ReadAllText(path));
            }
            _rangeParser = new MonikerRangeParser(monikerDefinition);
            _monikerAscendingComparer = new Lazy<MonikerComparer>(() => new MonikerComparer(monikerDefinition));
            _monikerDescendingComparer = new Lazy<MonikerComparer>(() => new MonikerComparer(monikerDefinition, false));
        }

        public IReadOnlyList<string> GetRangeMonikers(string rangeString)
            => _rangeParser.Parse(rangeString);

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
                    configMonikers.AddRange(GetRangeMonikers(monikerRange));
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

                var fileMonikers = GetRangeMonikers(fileMonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers, StringComparer.OrdinalIgnoreCase).ToList();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Count == 0)
                {
                    error = Errors.EmptyMonikers($"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is '{string.Join(',', configMonikers)}', while file moniker range '{fileMonikerRange}' is '{string.Join(',', fileMonikers)}'");
                }
                intersection.Sort(AscendingComparer);
                return (error, intersection);
            }

            configMonikers.Sort(AscendingComparer);
            return (error, configMonikers);
        }

        public List<string> GetZoneMonikers(string rangeString, List<string> fileLevelMonikers, List<Error> errors)
        {
            var monikers = new List<string>();

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Count == 0)
            {
                errors.Add(Errors.MonikerConfigMissing());
                return new List<string>();
            }

            var zoneLevelMonikers = GetRangeMonikers(rangeString);
            monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToList();

            if (monikers.Count == 0)
            {
                errors.Add(Errors.EmptyMonikers($"No intersection between zone and file level monikers. The result of zone level range string '{rangeString}' is '{string.Join(',', zoneLevelMonikers)}', while file level monikers is '{string.Join(',', fileLevelMonikers)}'."));
            }
            monikers.Sort(AscendingComparer);
            return monikers;
        }
    }
}
