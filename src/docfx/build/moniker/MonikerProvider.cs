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

        private readonly ConcurrentDictionary<FilePath, (List<Error>, MonikerList)> _monikerCache
                   = new ConcurrentDictionary<FilePath, (List<Error>, MonikerList)>();

        private readonly IReadOnlyDictionary<string, int> _monikerOrder;

        public MonikerProvider(Config config, BuildScope buildScope, MetadataProvider metadataProvider, FileResolver fileResolver)
        {
            _config = config;
            _buildScope = buildScope;
            _metadataProvider = metadataProvider;

            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(_config.MonikerDefinition))
            {
                var content = fileResolver.ReadString(_config.MonikerDefinition);
                monikerDefinition = JsonUtility.DeserializeData<MonikerDefinitionModel>(content, new FilePath(_config.MonikerDefinition));
            }
            _rangeParser = new MonikerRangeParser(monikerDefinition);

            _rules = _config.MonikerRange.Select(pair => (GlobUtility.CreateGlobMatcher(pair.Key), pair.Value)).Reverse().ToArray();
            _monikerOrder = GetMonikerOrder(monikerDefinition);
        }

        public int GetMonikerOrder(string moniker)
        {
            if (_monikerOrder.TryGetValue(moniker, out var value))
            {
                return value;
            }
            return 0;
        }

        public SourceInfo<string?> GetConfigMonikerRange(FilePath file)
        {
            // Fast pass to get config moniker range if the docset doesn't have any moniker config
            if (_rules.Length == 0 && _config.Groups.Count == 0 && _config.Content.All(x => x.Version.Value is null))
            {
                return default;
            }

            return _monikerRangeCache.GetOrAdd(file, GetConfigMonikerRangeCore);
        }

        public (List<Error> errors, MonikerList monikers) GetFileLevelMonikers(FilePath file)
        {
            return _monikerCache.GetOrAdd(file, GetFileLevelMonikersCore);
        }

        public (List<Error> errors, MonikerList monikers) GetZoneLevelMonikers(FilePath file, SourceInfo<string?> rangeString)
        {
            var errors = new List<Error>();
            var configMonikerRange = GetConfigMonikerRange(file);
            var (_, fileLevelMonikers) = GetFileLevelMonikers(file);

            // For conceptual docset,
            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (!_config.SkipMonikerValidation && configMonikerRange.Value is null)
            {
                errors.Add(Errors.Versioning.MonikerRangeUndefined(rangeString));
                return (errors, default);
            }

            var (zoneErrors, zoneLevelMonikers) = _rangeParser.Parse(rangeString);
            errors.AddRange(zoneErrors);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers);

            if (!monikers.HasMonikers)
            {
                errors.Add(Errors.Versioning.MonikerZoneEmpty(rangeString, zoneLevelMonikers, fileLevelMonikers));
                return (errors, monikers);
            }
            return (errors, monikers);
        }

        private (List<Error> errors, MonikerList monikers) GetFileLevelMonikersCore(FilePath file)
        {
            var errors = new List<Error>();
            var (_, metadata) = _metadataProvider.GetMetadata(file);

            var configMonikerRange = GetConfigMonikerRange(file);
            var (monikerErrors, configMonikers) = _rangeParser.Parse(configMonikerRange);
            errors.AddRange(monikerErrors);

            if (TryGetFileMonikers(metadata, out var fileMonikerErrors, out var fileMonikers, out var source))
            {
                errors.AddRange(fileMonikerErrors);

                if (metadata.MonikerRange != null)
                {
                    // For conceptual docset,
                    // Moniker range not defined in docfx.yml/docfx.json,
                    // user should not define it in file metadata
                    if (!_config.SkipMonikerValidation && configMonikerRange.Value is null)
                    {
                        errors.Add(Errors.Versioning.MonikerRangeUndefined(metadata.MonikerRange.Source));
                        return (errors, configMonikers);
                    }
                }

                var (intersectionError, intersection) =
                    GetMonikerIntersection(configMonikerRange, configMonikers, fileMonikers, source, _config.SkipMonikerValidation);
                errors.AddIfNotNull(intersectionError);
                return (errors, intersection);
            }

            return (errors, configMonikers);
        }

        private bool TryGetFileMonikers(UserMetadata metadata, out List<Error> errors, out MonikerList fileMonikers, out SourceInfo? source)
        {
            errors = new List<Error>();
            fileMonikers = default;
            source = default;
            List<Error> fileMonikerErrors;

            // if replace_monikers is set, the other moniker related metadata will be ignored
            if (metadata.ReplaceMonikers != null)
            {
                source = metadata.ReplaceMonikers.FirstOrDefault();
                (fileMonikerErrors, fileMonikers) = _rangeParser.Validate(metadata.ReplaceMonikers);
                errors.AddRange(fileMonikerErrors);
                return true;
            }

            MonikerList excludeMonikers = default;
            if (metadata.ExcludeMonikers != null)
            {
                List<Error> excludeMonikerErrors;
                (excludeMonikerErrors, excludeMonikers) = _rangeParser.Validate(metadata.ExcludeMonikers);
                errors.AddRange(excludeMonikerErrors);
            }
            if (metadata.MonikerRange != null)
            {
                // monikerRange takes precedence over monikers since it is more likely from user configuration
                if (metadata.Monikers != null)
                {
                    errors.Add(Errors.Versioning.DuplicateMonikerConfig(metadata.Monikers.FirstOrDefault()));
                }
                source = metadata.MonikerRange;

                (fileMonikerErrors, fileMonikers) = _rangeParser.Parse(metadata.MonikerRange);
                errors.AddRange(fileMonikerErrors);
                fileMonikers = fileMonikers.Except(excludeMonikers);
                return true;
            }
            else if (metadata.Monikers != null)
            {
                source = metadata.Monikers.FirstOrDefault();
                (fileMonikerErrors, fileMonikers) = _rangeParser.Validate(metadata.Monikers);
                errors.AddRange(fileMonikerErrors);
                fileMonikers = fileMonikers.Except(excludeMonikers);
                return true;
            }
            return false;
        }

        private (Error?, MonikerList) GetMonikerIntersection(
            SourceInfo<string?> configMonikerRange,
            MonikerList configMonikers,
            MonikerList fileMonikers,
            SourceInfo? source,
            bool skipMonikerValidation)
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
            var intersection = configMonikers.Intersect(fileMonikers);
            if (!intersection.HasMonikers)
            {
                error = Errors.Versioning.MonikeRangeOutOfScope(configMonikerRange, configMonikers, fileMonikers, source);
            }

            return (error, intersection);
        }

        private SourceInfo<string?> GetConfigMonikerRangeCore(FilePath file)
        {
            var (_, mapping) = _buildScope.MapPath(file.Path);

            if (mapping != null)
            {
                if (mapping.Version.Value != null)
                {
                    return mapping.Version;
                }
                else if (mapping.Group != null && _config.Groups.TryGetValue(mapping.Group, out var group))
                {
                    return group.MonikerRange;
                }
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

        private Dictionary<string, int> GetMonikerOrder(MonikerDefinitionModel monikerDefinition)
        {
            var result = new Dictionary<string, int>();
            var sorted = monikerDefinition.Monikers.OrderBy(moniker => moniker.Order).ToArray();
            for (var i = 0; i < sorted.Length; i++)
            {
                result[sorted[i].MonikerName] = i;
            }
            return result;
        }
    }
}
