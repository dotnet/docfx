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
        private readonly ErrorBuilder _errors;
        private readonly MetadataProvider _metadataProvider;
        private readonly FileResolver _fileResolver;
        private readonly LinkResolver _linkResolver;

        private readonly ConcurrentDictionary<FilePath, FilePath?> _zonePivotDefinitionFileCache = new ConcurrentDictionary<FilePath, FilePath?>();
        private readonly ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?> _zonePivotDefinitionModelCache =
            new ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?>();

        public ZonePivotProvider(
            ErrorBuilder errors,
            MetadataProvider metadataProvider,
            FileResolver fileResolver,
            LinkResolver linkResolver)
        {
            _errors = errors;
            _metadataProvider = metadataProvider;
            _fileResolver = fileResolver;
            _linkResolver = linkResolver;
        }

        public ZonePivotGroup? TryGetZonePivotGroup(FilePath file)
        {
            var groupId = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroups;
            if (groupId != null)
            {
                return TryGetZonePivotGroup(file, groupId);
            }
            else
            {
                return null;
            }
        }

        internal ZonePivotGroup? TryGetZonePivotGroup(FilePath file, string pivotGroupId)
        {
            var zonePivotGroupDefinition = GetZonePivotGroupDefinitionModel(file);
            if (zonePivotGroupDefinition != null)
            {
                var group = zonePivotGroupDefinition.Groups.Where(group => group.Id == pivotGroupId).FirstOrDefault();
                if (group == null)
                {
                    _errors.Add(Errors.ZonePivot.ZonePivotGroupNotFound(new SourceInfo(file), pivotGroupId, GetZonePivotGroupDefinitionFile(file)!));
                }

                return group;
            }
            else
            {
                return null;
            }
        }

        internal ZonePivotGroupDefinitionModel? GetZonePivotGroupDefinitionModel(FilePath file)
        {
            return _zonePivotDefinitionModelCache.GetOrAdd(file, _ => GetZonePivotGroupDefinitionModelCore(file));
        }

        internal ZonePivotGroupDefinitionModel? GetZonePivotGroupDefinitionModelCore(FilePath file)
        {
            var zonePivotGroupFilename = GetZonePivotGroupDefinitionFile(file);
            SourceInfo<string> source = zonePivotGroupFilename != null ? new SourceInfo<string>(zonePivotGroupFilename.Path) : default;
            if (zonePivotGroupFilename != null && _fileResolver.TryReadString(source, out var content))
            {
                var zonePivotGroupDefinition = YamlUtility.DeserializeData<ZonePivotGroupDefinitionModel>(content!, zonePivotGroupFilename);
                NormalizeZonePivotGroupDefinitionModel(zonePivotGroupDefinition, source);
                return zonePivotGroupDefinition;
            }
            else
            {
                _errors.Add(Errors.ZonePivot.ZonePivotGroupDefinitionNotFound(file));
                return null;
            }
        }

        internal FilePath? GetZonePivotGroupDefinitionFile(FilePath file)
        {
            return _zonePivotDefinitionFileCache.GetOrAdd(file, _ =>
                {
                    var definitionFile = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroupFilename;
                    var (error, f) = definitionFile != null ?
                        _linkResolver.ResolveContent(new SourceInfo<string>(definitionFile), file) :
                        (null, new FilePath("zone-pivot-groups.yml"));
                    if (error != null)
                    {
                        _errors.Add(error);
                    }

                    return f;
                });
        }

        internal void NormalizeZonePivotGroupDefinitionModel(ZonePivotGroupDefinitionModel model, SourceInfo? source)
        {
            var set = new HashSet<string>();
            foreach (var group in model.Groups)
            {
                if (set.Contains(group.Id))
                {
                    _errors.Add(Errors.ZonePivot.DuplicatedPivotGroups(source, group.Id));
                    model.Groups.Remove(group);
                }
                else
                {
                    set.Add(group.Id);
                    NormalizeZonePivotGroup(group, source);
                }
            }
        }

        internal void NormalizeZonePivotGroup(ZonePivotGroup group, SourceInfo? source)
        {
            var set = new HashSet<string>();
            foreach (var pivot in group.Pivots)
            {
                if (set.Contains(pivot.Id))
                {
                    _errors.Add(Errors.ZonePivot.DuplicatedPivotIds(source, pivot.Id, group.Id));
                    group.Pivots.Remove(pivot);
                }
                else
                {
                    set.Add(pivot.Id);
                }
            }
        }
    }
}
