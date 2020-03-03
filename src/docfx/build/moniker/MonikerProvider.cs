// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class MonikerProvider
    {
        private readonly Config _config;
        private readonly BuildScope _buildScope;
        private readonly MonikerRangeParser _rangeParser;
        private readonly MetadataProvider _metadataProvider;

        private readonly (Func<string, bool> glob, SourceInfo<string>)[] _rules;

        private readonly ConcurrentDictionary<FilePath, SourceInfo<string>> _monikerRangeCache
                   = new ConcurrentDictionary<FilePath, SourceInfo<string>>();

        private readonly ConcurrentDictionary<FilePath, (Error?, IReadOnlyList<string>)> _monikerCache
                   = new ConcurrentDictionary<FilePath, (Error?, IReadOnlyList<string>)>();

        public MonikerComparer Comparer { get; }

        public MonikerProvider(Config config, BuildScope buildScope, MetadataProvider metadataProvider, FileResolver fileResolver)
        {
            _config = config;
            _buildScope = buildScope;
            _metadataProvider = metadataProvider;

            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(_config.MonikerDefinition))
            {
                var content = fileResolver.ReadString(_config.MonikerDefinition);
                monikerDefinition = JsonUtility.Deserialize<MonikerDefinitionModel>(content, new FilePath(_config.MonikerDefinition));
            }
            var monikersEvaluator = new EvaluatorWithMonikersVisitor(monikerDefinition);
            _rangeParser = new MonikerRangeParser(monikersEvaluator);
            Comparer = new MonikerComparer(monikersEvaluator.MonikerOrder);

            _rules = _config.MonikerRange.Select(pair => (GlobUtility.CreateGlobMatcher(pair.Key), pair.Value)).Reverse().ToArray();
        }

        public SourceInfo<string> GetConfigMonikerRange(FilePath file)
        {
            // Fast pass to get config moniker range if the docset doesn't have any moniker config
            if (_rules.Length == 0 && _config.Groups.Count == 0)
            {
                return default;
            }

            return _monikerRangeCache.GetOrAdd(file, GetConfigMonikerRangeCore);
        }

        public (Error? error, IReadOnlyList<string> monikers) GetFileLevelMonikers(FilePath file)
        {
            return _monikerCache.GetOrAdd(file, GetFileLevelMonikersCore);
        }

        public (Error? error, IReadOnlyList<string> monikers) GetZoneLevelMonikers(FilePath file, SourceInfo<string> rangeString)
        {
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Count == 0)
            {
                return (Errors.MonikerRangeUndefined(rangeString), Array.Empty<string>());
            }

            var zoneLevelMonikers = _rangeParser.Parse(rangeString);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToArray();

            if (monikers.Length == 0)
            {
                var error = Errors.MonikeRangeOutOfScope(rangeString, zoneLevelMonikers, fileLevelMonikers);
                return (error, monikers);
            }
            return (null, monikers);
        }

        private (Error? error, IReadOnlyList<string> monikers) GetFileLevelMonikersCore(FilePath file)
        {
            var (_, metadata) = _metadataProvider.GetMetadata(file);

            var configMonikerRange = GetConfigMonikerRange(file);
            var configMonikers = _rangeParser.Parse(configMonikerRange);

            if (!string.IsNullOrEmpty(metadata.MonikerRange))
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (configMonikers.Length == 0)
                {
                    return (Errors.MonikerRangeUndefined(metadata.MonikerRange), configMonikers);
                }

                var fileMonikers = _rangeParser.Parse(metadata.MonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers).ToArray();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Length == 0)
                {
                    var error = Errors.MonikeRangeOutOfScope(configMonikerRange, configMonikers, metadata.MonikerRange, fileMonikers);
                    return (error, intersection);
                }
                return (null, intersection);
            }

            return (null, configMonikers);
        }

        private SourceInfo<string> GetConfigMonikerRangeCore(FilePath file)
        {
            var (_, mapping) = _buildScope.MapPath(file.Path);

            if (mapping != null && _config.Groups.TryGetValue(mapping.Group, out var group))
            {
                return group.MonikerRange;
            }

            foreach (var (glob, monikerRange) in _rules)
            {
                if (glob(file.Path))
                {
                    return monikerRange;
                }
            }

            return default;
        }
    }
}
