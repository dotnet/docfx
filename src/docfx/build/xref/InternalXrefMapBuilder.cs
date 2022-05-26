// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class InternalXrefMapBuilder
{
    private readonly Config _config;
    private readonly ErrorBuilder _errors;
    private readonly DocumentProvider _documentProvider;
    private readonly MetadataProvider _metadataProvider;
    private readonly MonikerProvider _monikerProvider;
    private readonly BuildScope _buildScope;
    private readonly RepositoryProvider _repositoryProvider;
    private readonly Input _input;
    private readonly Func<JsonSchemaTransformer> _jsonSchemaTransformer;
    private readonly RedirectionProvider _redirectionProvider;

    public InternalXrefMapBuilder(
        Config config,
        ErrorBuilder errors,
        DocumentProvider documentProvider,
        MetadataProvider metadataProvider,
        MonikerProvider monikerProvider,
        BuildScope buildScope,
        RepositoryProvider repositoryProvider,
        Input input,
        RedirectionProvider redirectionProvider,
        Func<JsonSchemaTransformer> jsonSchemaTransformer)
    {
        _config = config;
        _errors = errors;
        _documentProvider = documentProvider;
        _metadataProvider = metadataProvider;
        _monikerProvider = monikerProvider;
        _buildScope = buildScope;
        _repositoryProvider = repositoryProvider;
        _input = input;
        _jsonSchemaTransformer = jsonSchemaTransformer;
        _redirectionProvider = redirectionProvider;
    }

    public (IReadOnlyDictionary<string, InternalXrefSpec[]> xrefsByUid, IReadOnlyDictionary<FilePath, InternalXrefSpec[]> xrefsByFilePath) Build()
    {
        var fileXrefSpecMap = new ConcurrentDictionary<FilePath, InternalXrefSpec[]>();
        var builder = new ListBuilder<InternalXrefSpec>();

        using (var scope = Progress.Start("Building xref map"))
        {
            ParallelUtility.ForEach(scope, _errors, _buildScope.GetFiles(ContentType.Page), file => Load(_errors, builder, file, fileXrefSpecMap));
        }

        var xrefmap =
            from spec in builder.AsList()
            group spec by spec.Uid.Value into g
            let uid = g.Key
            let spec = AggregateXrefSpecs(uid, g.ToArray())
            select (uid, spec);

        var xrefsByUid = xrefmap.ToDictionary(item => item.uid, item => item.spec);
        xrefsByUid.TrimExcess();

        var xrefsByFilePath = fileXrefSpecMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        xrefsByFilePath.TrimExcess();

        return (xrefsByUid, xrefsByFilePath);
    }

    private void Load(
        ErrorBuilder errors,
        ListBuilder<InternalXrefSpec> xrefs,
        FilePath file,
        ConcurrentDictionary<FilePath, InternalXrefSpec[]> fileXrefSpecMap)
    {
        // if the file is already redirected, it should be excluded from xref map
        if (_redirectionProvider.TryGetValue(file.Path, out _))
        {
            return;
        }
        switch (file.Format)
        {
            case FileFormat.Markdown:
                var fileMetadata = _metadataProvider.GetMetadata(errors, file);
                var spec = LoadMarkdown(errors, fileMetadata, file);
                if (spec != null)
                {
                    fileXrefSpecMap.TryAdd(file, new InternalXrefSpec[] { spec });
                    xrefs.Add(spec);
                }
                break;

            case FileFormat.Yaml:
            case FileFormat.Json:
                var specs = _jsonSchemaTransformer().LoadXrefSpecs(errors, file);
                fileXrefSpecMap.TryAdd(file, specs.ToArray());
                xrefs.AddRange(specs);
                break;
        }
    }

    private InternalXrefSpec? LoadMarkdown(ErrorBuilder errors, UserMetadata metadata, FilePath file)
    {
        if (string.IsNullOrEmpty(metadata.Uid))
        {
            return default;
        }

        var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
        var xref = new InternalXrefSpec(metadata.Uid, _documentProvider.GetSiteUrl(file), file, monikers);

        xref.IsNameLocalizable = !string.IsNullOrEmpty(metadata.Title);
        xref.XrefProperties["name"] = new Lazy<JToken>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title.Value));

        return xref;
    }

    private InternalXrefSpec[] AggregateXrefSpecs(string uid, InternalXrefSpec[] specsWithSameUid)
    {
        // no conflicts
        if (specsWithSameUid.Length == 1)
        {
            return specsWithSameUid;
        }

        // loc override fallback uid
        if (specsWithSameUid.Any(spec => spec.DeclaringFile.Origin == FileOrigin.Main))
        {
            specsWithSameUid = specsWithSameUid.Where(spec => spec.DeclaringFile.Origin != FileOrigin.Fallback).ToArray();
        }

        // multiple uid conflicts
        // log an warning and take the first one order by the declaring file
        var duplicateGroups = specsWithSameUid.GroupBy(spec => spec.Monikers);
        var duplicateSpecs = new HashSet<InternalXrefSpec>();
        foreach (var monikerGroup in duplicateGroups)
        {
            var specsWithSameMonikerList = monikerGroup.ToList();
            if (specsWithSameMonikerList.Count > 1)
            {
                var duplicateSource = (from spec in specsWithSameMonikerList where spec.Uid.Value != null select spec.Uid.Source).ToArray();

                foreach (var spec in specsWithSameMonikerList)
                {
                    duplicateSpecs.Add(spec);
                    _errors.Add(Errors.Xref.DuplicateUid(spec.Uid, duplicateSource, spec.PropertyPath) with
                    { Level = _config.IsLearn ? ErrorLevel.Error : ErrorLevel.Warning, });
                }
            }
        }

        var conflictsWithoutDuplicated = specsWithSameUid.Where(spec => !duplicateSpecs.Contains(spec)).ToArray();

        // when the MonikerList is not equal but overlapped, log an moniker-overlapping warning and take the first one order by the declaring file
        if (CheckOverlappingMonikers(conflictsWithoutDuplicated, out var overlappingMonikers))
        {
            _errors.Add(Errors.Versioning.MonikerOverlapping(uid, specsWithSameUid.Select(spec => spec.DeclaringFile).ToList(), overlappingMonikers));
        }

        var specs = specsWithSameUid
               .OrderByDescending(spec => spec.Monikers.HasMonikers
                    ? spec.Monikers.Select(moniker => _monikerProvider.GetMonikerOrder(moniker)).Max()
                    : int.MaxValue);

        // TODO: clean up after new loc pipeline
        specs = _config.IsLearn
            ? specs.ThenByDescending(spec => GetUpdateTime(spec.DeclaringFile))
            : specs.ThenBy(spec => spec.DeclaringFile);
        return specs.ToArray();
    }

    private DateTime GetUpdateTime(FilePath file)
    {
        var fullPath = _input.TryGetOriginalPhysicalPath(file);
        if (fullPath is null)
        {
            return default;
        }

        var commits = _repositoryProvider.GetCommitHistory(fullPath.Value).commits;
        return commits.FirstOrDefault()?.Time.UtcDateTime ?? default;
    }

    private static bool CheckOverlappingMonikers(InternalXrefSpec[] specsWithSameUid, out HashSet<string> overlappingMonikers)
    {
        var isOverlapping = false;
        overlappingMonikers = new HashSet<string>();
        var monikerHashSet = new HashSet<string>();

        foreach (var spec in specsWithSameUid)
        {
            foreach (var moniker in spec.Monikers)
            {
                if (!monikerHashSet.Add(moniker))
                {
                    overlappingMonikers.Add(moniker);
                    isOverlapping = true;
                }
            }
        }
        return isOverlapping;
    }
}
