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
        private readonly Config _config;
        private readonly BuildScope _buildScope;
        private readonly MonikerRangeParser _rangeParser;
        private readonly MetadataProvider _metadataProvider;

        private readonly (Func<string, bool> glob, SourceInfo<string?>)[] _rules;

        private readonly ConcurrentDictionary<FilePath, SourceInfo<string?>> _monikerRangeCache
                   = new ConcurrentDictionary<FilePath, SourceInfo<string?>>();

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

        public SourceInfo<string?> GetConfigMonikerRange(FilePath file)
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

        public (Error? error, string[] monikers) GetZoneLevelMonikers(FilePath file, SourceInfo<string?> rangeString)
        {
            var configMonikerRange = GetConfigMonikerRange(file);
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // For conceptual docset,
            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (!_config.SkipMonikerValidation && configMonikerRange.Value is null)
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
            var configMonikers = _rangeParser.Parse(configMonikerRange);

            if (metadata.MonikerRange != null)
            {
                // For conceptual docet,
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (!_config.SkipMonikerValidation && configMonikerRange.Value is null)
                {
                    errors.Add(Errors.Versioning.MonikerRangeUndefined(metadata.MonikerRange.Source));
                    return (errors, configMonikers);
                }

                // monikerRange takes precedence over monikers since it is more likely from user configuration
                if (metadata.Monikers != null)
                {
                    errors.Add(Errors.Versioning.DuplicateMonikerConfig(metadata.Monikers.FirstOrDefault()));
                }

                var fileMonikers = _rangeParser.Parse(metadata.MonikerRange);
                var (intersectionError, intersection) = GetMonikerIntersection(metadata, configMonikerRange, configMonikers, fileMonikers, _config.SkipMonikerValidation);
                errors.AddIfNotNull(intersectionError);
                return (errors, intersection);
            }
            else if (metadata.Monikers != null)
            {
                var fileMonikers = metadata.Monikers.SelectMany(x => _rangeParser.Parse(x)).ToArray();
                var (intersectionError, intersection) = GetMonikerIntersection(metadata, configMonikerRange, configMonikers, fileMonikers, _config.SkipMonikerValidation);
                errors.AddIfNotNull(intersectionError);
                return (errors, intersection);
            }

            return (errors, configMonikers);
        }

        private static (Error?, string[]) GetMonikerIntersection(UserMetadata metadata, SourceInfo<string?> configMonikerRange, string[] configMonikers, string[] fileMonikers, bool skipMonikerValidation)
        {
            Error? error = null;

            // for reference docset, if config monikers is not defined
            // just use file monikers
            if (skipMonikerValidation && configMonikerRange.Value is null)
            {
                return (error, fileMonikers);
            }

            // With config monikers defined,
            // warn if no intersection of config monikers and file monikers
            var intersection = configMonikers.Intersect(fileMonikers).ToArray();
            if (intersection.Length == 0)
            {
                if (!string.IsNullOrEmpty(metadata.MonikerRange))
                {
                    error = Errors.Versioning.MonikeRangeOutOfScope(configMonikerRange, configMonikers, metadata.MonikerRange, fileMonikers);
                }
                else if (metadata.Monikers != null)
                {
                    error = Errors.Versioning.MonikeRangeOutOfScope(configMonikerRange, configMonikers, metadata.Monikers, fileMonikers);
                }
            }

            return (error, intersection);
        }

        private SourceInfo<string?> GetConfigMonikerRangeCore(FilePath file)
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
