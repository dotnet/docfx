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

        private readonly ConcurrentDictionary<FilePath, SourceInfo<string?>> _monikerRangeCache = new ConcurrentDictionary<FilePath, SourceInfo<string?>>();
        private readonly ConcurrentDictionary<FilePath, MonikerList> _monikerCache = new ConcurrentDictionary<FilePath, MonikerList>();
        private readonly ConcurrentDictionary<FilePath, MonikerList> _excludeMonikerCache = new ConcurrentDictionary<FilePath, MonikerList>();

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

        public MonikerList Validate(ErrorBuilder errors, SourceInfo<string>[] monikers) => _rangeParser.Validate(errors, monikers);

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

        public MonikerList GetFileLevelMonikers(ErrorBuilder errors, FilePath file)
        {
            return _monikerCache.GetOrAdd(file, key => GetFileLevelMonikersCore(errors, key));
        }

        public MonikerList GetZoneLevelMonikers(ErrorBuilder errors, FilePath file, SourceInfo<string?> rangeString)
        {
            var configMonikerRange = GetConfigMonikerRange(file);
            var fileLevelMonikers = GetFileLevelMonikers(errors, file);

            // For conceptual docset,
            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (file.Format == FileFormat.Markdown && configMonikerRange.Value is null)
            {
                errors.Add(Errors.Versioning.MonikerRangeUndefined(rangeString));
                return default;
            }

            var zoneLevelMonikers = _rangeParser.Parse(errors, rangeString);
            var monikers = fileLevelMonikers.Intersect(zoneLevelMonikers);

            if (_excludeMonikerCache.TryGetValue(file, out var excludeMonikers))
            {
                if (!fileLevelMonikers.Concat(excludeMonikers).Intersect(zoneLevelMonikers).HasMonikers)
                {
                    errors.Add(Errors.Versioning.MonikerZoneEmpty(rangeString, zoneLevelMonikers, fileLevelMonikers));
                }
            }
            else if (!monikers.HasMonikers)
            {
                errors.Add(Errors.Versioning.MonikerZoneEmpty(rangeString, zoneLevelMonikers, fileLevelMonikers));
            }
            return monikers;
        }

        private MonikerList GetFileLevelMonikersCore(ErrorBuilder errors, FilePath file)
        {
            if (file.Monikers.HasMonikers)
            {
                return file.Monikers;
            }

            var metadata = _metadataProvider.GetMetadata(errors, file);
            var configMonikerRange = GetConfigMonikerRange(file);
            var configMonikers = _rangeParser.Parse(errors, configMonikerRange);
            return GetFileLevelMonikers(errors, configMonikerRange, configMonikers, metadata, file);
        }

        private MonikerList GetFileLevelMonikers(
            ErrorBuilder errors,
            SourceInfo<string?> configMonikerRange,
            MonikerList configMonikers,
            UserMetadata metadata,
            FilePath file)
        {
            if (metadata.MonikerRange != null)
            {
                // For conceptual docset,
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (file.Format == FileFormat.Markdown && configMonikerRange.Value is null)
                {
                    errors.Add(Errors.Versioning.MonikerRangeUndefined(metadata.MonikerRange.Source));
                    return default;
                }
            }

            SourceInfo? source = default;
            MonikerList fileMonikers;

            // if replace_monikers is set, the other moniker related metadata will be ignored
            if (metadata.ReplaceMonikers != null)
            {
                source = metadata.ReplaceMonikers.FirstOrDefault();
                fileMonikers = _rangeParser.Validate(errors, metadata.ReplaceMonikers);
            }
            else if (metadata.MonikerRange != null)
            {
                // monikerRange takes precedence over monikers since it is more likely from user configuration
                if (metadata.Monikers != null)
                {
                    errors.Add(Errors.Versioning.DuplicateMonikerConfig(metadata.Monikers.FirstOrDefault()));
                }
                source = metadata.MonikerRange;
                fileMonikers = _rangeParser.Parse(errors, metadata.MonikerRange);
            }
            else if (metadata.Monikers != null)
            {
                source = metadata.Monikers.FirstOrDefault();
                fileMonikers = _rangeParser.Validate(errors, metadata.Monikers);
            }
            else
            {
                fileMonikers = configMonikers;
            }

            MonikerList excludeMonikers = default;
            if (metadata.ExcludeMonikers != null)
            {
                excludeMonikers = _excludeMonikerCache.GetOrAdd(file, _rangeParser.Validate(errors, metadata.ExcludeMonikers));
            }
            fileMonikers = fileMonikers.Except(excludeMonikers);

            // for non-markdown documents, if config monikers is not defined
            // just use file monikers
            if (configMonikerRange.Value is null && file.Format != FileFormat.Markdown)
            {
                return fileMonikers;
            }

            if (configMonikers.HasMonikers || fileMonikers.HasMonikers)
            {
                // With config monikers defined,
                // warn if no intersection of config monikers and file monikers
                var intersection = configMonikers.Intersect(fileMonikers);
                if (!intersection.HasMonikers)
                {
                    errors.Add(Errors.Versioning.MonikeRangeOutOfScope(configMonikerRange, configMonikers, fileMonikers, source));
                }
                fileMonikers = intersection;
            }
            return fileMonikers;
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

        private static Dictionary<string, int> GetMonikerOrder(MonikerDefinitionModel monikerDefinition)
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
