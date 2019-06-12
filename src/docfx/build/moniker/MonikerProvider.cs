// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MonikerProvider
    {
        private readonly List<(Func<string, bool> glob, (string monikerRange, IEnumerable<string> monikers))> _rules = new List<(Func<string, bool>, (string, IEnumerable<string>))>();
        private readonly MonikerRangeParser _rangeParser;
        private readonly MetadataProvider _metadataProvider;
        private readonly ConcurrentDictionary<Document, (Error, List<string>)> _monikerCache
                   = new ConcurrentDictionary<Document, (Error, List<string>)>();

        public MonikerComparer Comparer { get; }

        public MonikerProvider(Docset docset, MetadataProvider metadataProvider)
        {
            _metadataProvider = metadataProvider;

            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(docset.Config.MonikerDefinition))
            {
                var (_, content, _) = RestoreMap.GetRestoredFileContent(docset, docset.Config.MonikerDefinition);
                monikerDefinition = JsonUtility.Deserialize<MonikerDefinitionModel>(content, docset.Config.MonikerDefinition);
            }
            var monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
            _rangeParser = new MonikerRangeParser(monikersEvaluator);
            Comparer = new MonikerComparer(monikersEvaluator.MonikerOrder);

            foreach (var (key, monikerRange) in docset.Config.MonikerRange)
            {
                _rules.Add((GlobUtility.CreateGlobMatcher(key), (monikerRange, _rangeParser.Parse(monikerRange))));
            }
            _rules.Reverse();
        }

        public (Error error, List<string> monikers) GetFileLevelMonikers(Document file)
        {
            return _monikerCache.GetOrAdd(file, GetFileLevelMonikersCore);
        }

        public (Error error, List<string> monikers) GetZoneLevelMonikers(Document file, SourceInfo<string> rangeString)
        {
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Count == 0)
            {
                return (Errors.MonikerConfigMissing(), new List<string>());
            }

            var zoneLevelMonikers = _rangeParser.Parse(rangeString);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToList();

            if (monikers.Count == 0)
            {
                var error = Errors.EmptyMonikers($"No intersection between zone and file level monikers. The result of zone level range string '{rangeString}' is '{string.Join(',', zoneLevelMonikers)}', while file level monikers is '{string.Join(',', fileLevelMonikers)}'.");
                return (error, monikers);
            }
            monikers.Sort(Comparer);
            return (null, monikers);
        }

        private (Error error, List<string> monikers) GetFileLevelMonikersCore(Document file)
        {
            var errors = new List<Error>();
            var (_, metadata) = _metadataProvider.GetMetadataModel(file);

            string configMonikerRange = null;
            var configMonikers = new List<string>();

            foreach (var (glob, (monikerRange, monikers)) in _rules)
            {
                if (glob(file.FilePath))
                {
                    configMonikerRange = monikerRange;
                    configMonikers.AddRange(monikers);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(metadata.MonikerRange))
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (configMonikers.Count == 0)
                {
                    return (Errors.MonikerConfigMissing(), configMonikers);
                }

                var fileMonikers = _rangeParser.Parse(metadata.MonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers).ToList();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Count == 0)
                {
                    var error = Errors.EmptyMonikers($"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is '{string.Join(',', configMonikers)}', while file moniker range '{metadata.MonikerRange}' is '{string.Join(',', fileMonikers)}'");
                    return (error, intersection);
                }
                return (null, intersection);
            }

            return (null, configMonikers);
        }
    }
}
