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
        private readonly List<(Func<string, bool> glob, (string monikerRange, IEnumerable<string> monikers))>
            _rules = new List<(Func<string, bool>, (string, IEnumerable<string>))>();

        private readonly MonikerRangeParser _rangeParser;
        private readonly MetadataProvider _metadataProvider;
        private readonly ConcurrentDictionary<Document, (Error, IReadOnlyCollection<string>)> _monikerCache
                   = new ConcurrentDictionary<Document, (Error, IReadOnlyCollection<string>)>();

        public MonikerComparer Comparer { get; }

        public MonikerProvider(Docset docset, MetadataProvider metadataProvider, RestoreFileMap restoreFileMap)
        {
            _metadataProvider = metadataProvider;

            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(docset.Config.MonikerDefinition))
            {
                var content = restoreFileMap.GetRestoredFileContent(docset.Config.MonikerDefinition);
                monikerDefinition = JsonUtility.Deserialize<MonikerDefinitionModel>(content, new FilePath(docset.Config.MonikerDefinition));
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

        public (Error error, IReadOnlyCollection<string> monikers) GetFileLevelMonikers(Document file)
        {
            return _monikerCache.GetOrAdd(file, GetFileLevelMonikersCore);
        }

        public (Error error, IReadOnlyCollection<string> monikers) GetZoneLevelMonikers(Document file, SourceInfo<string> rangeString)
        {
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (!fileLevelMonikers.Any())
            {
                return (Errors.MonikerRangeUndefined(rangeString), Array.Empty<string>());
            }

            var zoneLevelMonikers = _rangeParser.Parse(rangeString);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToArray();

            if (!monikers.Any())
            {
                var error = Errors.MonikeRangeOutOfScope(rangeString, zoneLevelMonikers, fileLevelMonikers);
                return (error, monikers);
            }
            return (null, monikers);
        }

        private (Error error, IReadOnlyCollection<string> monikers) GetFileLevelMonikersCore(Document file)
        {
            var errors = new List<Error>();
            var (_, metadata) = _metadataProvider.GetMetadata(file);

            string configMonikerRange = null;
            var configMonikers = Array.Empty<string>();

            foreach (var (glob, (monikerRange, monikers)) in _rules)
            {
                if (glob(file.FilePath.Path))
                {
                    configMonikerRange = monikerRange;
                    configMonikers = monikers.ToArray();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(metadata.MonikerRange))
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (!configMonikers.Any())
                {
                    return (Errors.MonikerRangeUndefined(metadata.MonikerRange), configMonikers);
                }

                var fileMonikers = _rangeParser.Parse(metadata.MonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers).ToArray();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (!intersection.Any())
                {
                    var error = Errors.MonikeRangeOutOfScope(configMonikerRange, configMonikers, metadata.MonikerRange, fileMonikers);
                    return (error, intersection);
                }
                return (null, intersection);
            }

            return (null, configMonikers);
        }
    }
}
