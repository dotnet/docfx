// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal class ZonePivotProvider
{
    private const string DefaultDefinitionFile = "zone-pivot-groups.json";

    private readonly ErrorBuilder _errors;
    private readonly DocumentProvider _documentProvider;
    private readonly MetadataProvider _metadataProvider;
    private readonly Input _input;
    private readonly PublishUrlMap _publishUrlMap;
    private readonly Func<ContentValidator> _contentValidator;

    private readonly ConcurrentDictionary<FilePath, Watch<(FilePath?, ZonePivotGroupDefinition?)>> _zonePivotDefinitionCache = new();

    public ZonePivotProvider(
        ErrorBuilder errors,
        DocumentProvider documentProvider,
        MetadataProvider metadataProvider,
        Input input,
        PublishUrlMap publishUrlMap,
        Func<ContentValidator> contentValidator)
    {
        _errors = errors;
        _documentProvider = documentProvider;
        _metadataProvider = metadataProvider;
        _input = input;
        _publishUrlMap = publishUrlMap;
        _contentValidator = contentValidator;
    }

    public (FilePath DefinitionFile, List<ZonePivotGroup> PivotGroups)? TryGetZonePivotGroups(FilePath file)
    {
        var groupIds = _metadataProvider.GetMetadata(_errors, file).ZonePivotGroups?.Split(",");
        if (groupIds is null)
        {
            _errors.Add(Errors.ZonePivot.ZonePivotGroupNotSpecified(new SourceInfo(file)));
            return default;
        }

        var (definitionFile, zonePivotGroupDefinition) = GetZonePivotGroupDefinition(file);
        if (definitionFile is null || zonePivotGroupDefinition is null)
        {
            return default;
        }

        var groups = groupIds.Select(groupId => GetZonePivotGroup(file, groupId)).OfType<ZonePivotGroup>().ToList();
        if (groups.Any())
        {
            return (definitionFile, groups);
        }

        return default;

        ZonePivotGroup? GetZonePivotGroup(FilePath file, string pivotGroupId)
        {
            var group = zonePivotGroupDefinition.Groups.FirstOrDefault(group => group.Id == pivotGroupId);
            if (group is null)
            {
                _errors.Add(Errors.ZonePivot.ZonePivotGroupNotFound(new SourceInfo(file), pivotGroupId, definitionFile));
            }

            return group;
        }
    }

    private (FilePath?, ZonePivotGroupDefinition?) GetZonePivotGroupDefinition(FilePath file)
    {
        return _zonePivotDefinitionCache.GetOrAdd(file, key => new(() => GetZonePivotGroupDefinitionCore(key))).Value;
    }

    private (FilePath?, ZonePivotGroupDefinition?) GetZonePivotGroupDefinitionCore(FilePath file)
    {
        var zonePivotGroupDefinitionFile = GetZonePivotGroupDefinitionFile(file);
        if (zonePivotGroupDefinitionFile is null)
        {
            return default;
        }
        var zonePivotGroupDefinition = YamlUtility.DeserializeData<ZonePivotGroupDefinition>(
            _input.ReadString(zonePivotGroupDefinitionFile),
            zonePivotGroupDefinitionFile);
        _contentValidator().ValidateZonePivotDefinition(zonePivotGroupDefinitionFile, zonePivotGroupDefinition);
        return (zonePivotGroupDefinitionFile, zonePivotGroupDefinition);
    }

    private FilePath? GetZonePivotGroupDefinitionFile(FilePath file)
    {
        var publishUrl = GetZonePivotDefinitionPublishUrl(file, _metadataProvider.GetMetadata(_errors, file).ZonePivotGroupFilename);
        var files = _publishUrlMap.GetFilesByUrl(publishUrl).ToList();
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
#pragma warning disable CS0618 // Type or member is obsolete
                _documentProvider.GetDocsSiteUrl(file).Split('/', StringSplitOptions.RemoveEmptyEntries).First(),
#pragma warning restore CS0618 // Type or member is obsolete
                Path.ChangeExtension(definitionFilename ?? DefaultDefinitionFile, "json")));
    }
}
