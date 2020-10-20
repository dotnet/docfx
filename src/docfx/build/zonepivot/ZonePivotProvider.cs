// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class ZonePivotProvider
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly MetadataProvider _metadataProvider;
        private readonly Input _input;
        private readonly Lazy<PublishUrlMap> _publishUrlMap;

        private readonly ConcurrentDictionary<FilePath, FilePath?> _zonePivotDefinitionFileCache = new ConcurrentDictionary<FilePath, FilePath?>();
        private readonly ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?> _zonePivotDefinitionModelCache =
            new ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?>();

        public ZonePivotProvider(
            Config config,
            ErrorBuilder errors,
            MetadataProvider metadataProvider,
            Input input,
            Lazy<PublishUrlMap> publishUrlMap)
        {
            _config = config;
            _errors = errors;
            _metadataProvider = metadataProvider;
            _input = input;
            _publishUrlMap = publishUrlMap;
        }

        public (FilePath?, ZonePivotGroup?) GetZonePivotGroup(FilePath file)
        {
            var groupId = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroups;
            if (groupId != null)
            {
                return (GetZonePivotGroupDefinitionFile(file), GetZonePivotGroup(file, groupId));
            }
            else
            {
                return (null, null);
            }
        }

        private ZonePivotGroup? GetZonePivotGroup(FilePath file, string pivotGroupId)
        {
            var (definitionFile, zonePivotGroupDefinition) = GetZonePivotGroupDefinitionModel(file);
            if (zonePivotGroupDefinition != null)
            {
                var group = zonePivotGroupDefinition.Groups.Where(group => group.Id == pivotGroupId).FirstOrDefault();
                if (group == null)
                {
                    _errors.Add(Errors.ZonePivot.ZonePivotGroupNotFound(new SourceInfo(file), pivotGroupId, GetZonePivotGroupDefinitionFile(file)));
                }

                return group;
            }
            else
            {
                return null;
            }
        }

        private (FilePath?, ZonePivotGroupDefinitionModel?) GetZonePivotGroupDefinitionModel(FilePath file)
        {
            var definitionFile = GetZonePivotGroupDefinitionFile(file);
            return (definitionFile, definitionFile != null ? _zonePivotDefinitionModelCache.GetOrAdd(file, GetZonePivotGroupDefinitionModelCore) : null);
        }

        private ZonePivotGroupDefinitionModel? GetZonePivotGroupDefinitionModelCore(FilePath file)
        {
            var zonePivotGroupDefinitionFile = GetZonePivotGroupDefinitionFile(file);
            if (zonePivotGroupDefinitionFile != null)
            {
                var zonePivotGroupDefinition = YamlUtility.DeserializeData<ZonePivotGroupDefinitionModel>(
                    _input.ReadString(zonePivotGroupDefinitionFile),
                    zonePivotGroupDefinitionFile);
                NormalizeZonePivotGroupDefinitionModel(zonePivotGroupDefinition, new SourceInfo(zonePivotGroupDefinitionFile));
                return zonePivotGroupDefinition;
            }
            else
            {
                _errors.Add(Errors.ZonePivot.ZonePivotGroupDefinitionNotFound(file));
                return null;
            }
        }

        private FilePath? GetZonePivotGroupDefinitionFile(FilePath file)
        {
            return _zonePivotDefinitionFileCache.GetOrAdd(file, _ =>
                {
                    var definitionFile = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroupFilename ?? "zone-pivot-groups.json";
                    var files = _publishUrlMap.Value.GetFilesByUrl(UrlUtility.Combine(_config.BasePath.ValueWithLeadingSlash, definitionFile)).ToList();
                    if (files.Count != 1)
                    {
                        _errors.Add(Errors.ZonePivot.ZonePivotGroupDefinitionNotFound(file));
                        return null;
                    }
                    else
                    {
                        return files.First();
                    }
                });
        }

        private void NormalizeZonePivotGroupDefinitionModel(ZonePivotGroupDefinitionModel model, SourceInfo? source)
        {
            var set = new HashSet<string>();
            foreach (var group in model.Groups)
            {
                NormalizeZonePivotGroup(group, source);
                if (!set.Add(group.Id))
                {
                    _errors.Add(Errors.ZonePivot.DuplicatedPivotGroups(source, group.Id));
                }
            }
        }

        private void NormalizeZonePivotGroup(ZonePivotGroup group, SourceInfo? source)
        {
            var set = new HashSet<string>();
            foreach (var pivot in group.Pivots)
            {
                if (!set.Add(pivot.Id))
                {
                    _errors.Add(Errors.ZonePivot.DuplicatedPivotIds(source, pivot.Id, group.Id));
                }
            }
        }
    }
}
