// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class ZonePivotProvider
    {
        private const string DefaultDefinitionFile = "zone-pivot-groups.json";

        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly DocumentProvider _documentProvider;
        private readonly MetadataProvider _metadataProvider;
        private readonly Input _input;
        private readonly Lazy<PublishUrlMap> _publishUrlMap;

        private readonly ConcurrentDictionary<FilePath, FilePath?> _zonePivotDefinitionFileCache = new ConcurrentDictionary<FilePath, FilePath?>();
        private readonly ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?> _zonePivotDefinitionModelCache =
            new ConcurrentDictionary<FilePath, ZonePivotGroupDefinitionModel?>();

        public ZonePivotProvider(
            Config config,
            ErrorBuilder errors,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            Input input,
            Lazy<PublishUrlMap> publishUrlMap)
        {
            _config = config;
            _errors = errors;
            _documentProvider = documentProvider;
            _metadataProvider = metadataProvider;
            _input = input;
            _publishUrlMap = publishUrlMap;
        }

        public (FilePath DefinitionFile, List<ZonePivotGroup> PivotGroups)? TryGetZonePivotGroups(FilePath file)
        {
            var groupIds = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroups?.Split(",");
            if (groupIds != null)
            {
                var definitionFile = GetZonePivotGroupDefinitionFile(file);
                if (definitionFile != null)
                {
                    var groups = groupIds.Select(groupId => GetZonePivotGroup(file, groupId)).Where(p => p != null).OfType<ZonePivotGroup>().ToList();
                    return (definitionFile, groups);
                }
            }

            return null;
        }

        private ZonePivotGroup? GetZonePivotGroup(FilePath file, string pivotGroupId)
        {
            var zonePivotGroupDefinition = GetZonePivotGroupDefinitionModel(file);
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

        private ZonePivotGroupDefinitionModel? GetZonePivotGroupDefinitionModel(FilePath file)
        {
            var definitionFile = GetZonePivotGroupDefinitionFile(file);
            return definitionFile != null ? _zonePivotDefinitionModelCache.GetOrAdd(file, GetZonePivotGroupDefinitionModelCore) : null;
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
                return null;
            }
        }

        private FilePath? GetZonePivotGroupDefinitionFile(FilePath file)
        {
            return _zonePivotDefinitionFileCache.GetOrAdd(file, _ =>
                {
                    var publishUrl = GetZonePivotDefinitionPublishUrl(file, _metadataProvider.GetMetadata(_errors, file).ZonePivotGroupFilename);
                    var files = _publishUrlMap.Value.GetFilesByUrl(publishUrl).ToList();
                    switch (files.Count)
                    {
                        case 0:
                            _errors.Add(Errors.ZonePivot.ZonePivotGroupDefinitionNotFound(file, publishUrl));
                            return null;
                        case 1:
                            return files.First();
                        default:
                            _errors.Add(Errors.ZonePivot.ZonePivotGroupDefinitionConflict(file, publishUrl));
                            return null;
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

        /// <summary>
        /// Get publish URL of zone pivots definition file.
        /// </summary>
        /// <param name="file">Path of the file referencing zone pivot groups</param>
        /// <param name="definitionFilename">The definition filename from metadata, if null, use default "zone-pivot-groups.json".</param>
        /// <returns>Published URL of zone pivots definition file.</returns>
        private string GetZonePivotDefinitionPublishUrl(FilePath file, string? definitionFilename)
        {
            return "/" + PathUtility.NormalizeFile(UrlUtility.Combine(
                _documentProvider.GetDocsSiteUrl(file).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
                Path.ChangeExtension(definitionFilename ?? DefaultDefinitionFile, "json")));
        }
    }
}
