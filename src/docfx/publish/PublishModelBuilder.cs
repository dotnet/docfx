// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class PublishModelBuilder
{
    private readonly Config _config;
    private readonly ErrorBuilder _errors;
    private readonly MonikerProvider _monikerProvider;
    private readonly string _locale;
    private readonly SourceMap _sourceMap;
    private readonly DocumentProvider _documentProvider;
    private readonly ContributionProvider _contributionProvider;

    private readonly ConcurrentDictionary<FilePath, (JObject? metadata, string? outputPath)> _buildOutput = new();

    public PublishModelBuilder(
        Config config,
        ErrorBuilder errors,
        MonikerProvider monikerProvider,
        BuildOptions buildOptions,
        SourceMap sourceMap,
        DocumentProvider documentProvider,
        ContributionProvider contributionProvider)
    {
        _config = config;
        _errors = errors;
        _monikerProvider = monikerProvider;
        _locale = buildOptions.Locale;
        _sourceMap = sourceMap;
        _documentProvider = documentProvider;
        _contributionProvider = contributionProvider;
    }

    public void AddOrUpdate(FilePath file, JObject? metadata, string? outputPath)
    {
        _buildOutput.TryAdd(file, (metadata, outputPath));
    }

    public (PublishModel, Dictionary<FilePath, PublishItem>) Build(IReadOnlyCollection<FilePath> files)
    {
        var publishItems = new Dictionary<FilePath, PublishItem>();

        foreach (var sourceFile in files.Concat(_buildOutput.Keys))
        {
            if (!publishItems.ContainsKey(sourceFile))
            {
                _buildOutput.TryGetValue(sourceFile, out var buildOutput);

                var sourceFilePath = _sourceMap.GetOriginalFilePath(sourceFile)?.Path ?? sourceFile.Path;

                var publishItem = new PublishItem
                {
                    Url = _documentProvider.GetSiteUrl(sourceFile),
                    Path = buildOutput.outputPath,
                    SourceFile = sourceFile.Origin == FileOrigin.Redirection ? null : sourceFile,
                    SourcePath = sourceFile.Origin == FileOrigin.Redirection ? null : sourceFilePath,
                    SourceUrl = _contributionProvider.GetReportGitUrl(sourceFile),
                    Locale = _locale,
                    Monikers = _monikerProvider.GetFileLevelMonikers(_errors, sourceFile),
                    ConfigMonikerRange = _monikerProvider.GetConfigMonikerRange(sourceFile),
                    HasError = _errors.FileHasError(sourceFile),
                    ExtensionData = buildOutput.metadata,
                };

                publishItems.Add(sourceFile, publishItem);
            }
        }

        var items = (
               from item in publishItems.Values
               orderby item.Locale, item.Path, item.Url, item.MonikerGroup
               select item).ToArray();

        var monikerGroups = new Dictionary<string, MonikerList>(
            from item in publishItems.Values
            let monikerGroup = item.MonikerGroup
            where !string.IsNullOrEmpty(monikerGroup)
            orderby monikerGroup
            group item by monikerGroup into g
            select new KeyValuePair<string, MonikerList>(g.Key, g.First().Monikers));

        var model = new PublishModel
        {
            Name = _config.Name,
            Product = _config.Product,
            BasePath = _config.BasePath.ValueWithLeadingSlash,
            ThemeBranch = _config.Template.IsMainOrMaster ? null : _config.Template.Branch,
            Files = items,
            MonikerGroups = monikerGroups,
        };

        var fileManifests = publishItems.ToDictionary(item => item.Key, item => item.Value);

        return (model, fileManifests);
    }
}
