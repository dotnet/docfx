// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;

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

        private readonly ConcurrentDictionary<FilePath, (List<Error>, string[])> _monikerCache
                   = new ConcurrentDictionary<FilePath, (List<Error>, string[])>();

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

        public (List<Error> errors, string[] monikers) GetFileLevelMonikers(FilePath file)
        {
            return _monikerCache.GetOrAdd(file, GetFileLevelMonikersCore);
        }

        public (Error? error, string[] monikers) GetZoneLevelMonikers(FilePath file, SourceInfo<string> rangeString)
        {
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Length == 0)
            {
                return (Errors.Versioning.MonikerRangeUndefined(rangeString), Array.Empty<string>());
            }

            var zoneLevelMonikers = _rangeParser.Parse(rangeString);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToArray();

            if (monikers.Length == 0)
            {
                var error = Errors.Versioning.MonikeRangeOutOfScope(rangeString, zoneLevelMonikers, fileLevelMonikers);
                return (error, monikers);
            }
            return (null, monikers);
        }

        private (List<Error> errors, string[] monikers) GetFileLevelMonikersCore(FilePath file)
        {
            var errors = new List<Error>();
            var (_, metadata) = _metadataProvider.GetMetadata(file);

            var configMonikerRange = GetConfigMonikerRange(file);
            var configedMonikers = _rangeParser.Parse(configMonikerRange);

            if (!string.IsNullOrEmpty(metadata.MonikerRange) || metadata.Monikers != null)
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (configedMonikers.Length == 0)
                {
                    errors.Add(Errors.Versioning.MonikerRangeUndefined(metadata.MonikerRange));
                    return (errors, configedMonikers);
                }

                var fileMonikers = _rangeParser.Parse(metadata.MonikerRange);

                // monikerRagen takes precedence over monikers since it is more likely from user configuration
                if (metadata.MonikerRange != null && metadata.Monikers != null)
                {
                    var source = metadata.Monikers.First().Source;
                    if (source != null)
                    {
                        errors.Add(Errors.Versioning.DuplciateMonikerConfig(source));
                    }
                }
                if (string.IsNullOrEmpty(metadata.MonikerRange) && metadata.Monikers != null)
                {
                    fileMonikers = metadata.Monikers.SelectMany(x => _rangeParser.Parse(x)).ToArray();
                }

                var intersection = configedMonikers.Intersect(fileMonikers).ToArray();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Length == 0)
                {
                    errors.Add(Errors.Versioning.MonikeRangeOutOfScope(configMonikerRange, configedMonikers, fileMonikers, metadata.MonikerRange, metadata.Monikers));
                    return (errors, intersection);
                }
                return (errors, intersection);
            }

            return (errors, configedMonikers);
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
