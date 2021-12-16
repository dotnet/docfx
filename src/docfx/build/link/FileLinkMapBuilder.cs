// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class FileLinkMapBuilder
{
    private readonly ErrorBuilder _errors;
    private readonly DocumentProvider _documentProvider;
    private readonly MonikerProvider _monikerProvider;
    private readonly ContributionProvider _contributionProvider;

    private readonly Scoped<ConcurrentHashSet<FileLinkItem>> _links = new();

    public FileLinkMapBuilder(
        ErrorBuilder errors, DocumentProvider documentProvider, MonikerProvider monikerProvider, ContributionProvider contributionProvider)
    {
        _errors = errors;
        _documentProvider = documentProvider;
        _monikerProvider = monikerProvider;
        _contributionProvider = contributionProvider;
    }

    public void AddFileLink(FilePath inclusionRoot, FilePath referencingFile, string targetUrl, SourceInfo? source)
    {
        var sourceUrl = _documentProvider.GetSiteUrl(inclusionRoot);

        if (string.IsNullOrEmpty(targetUrl) || sourceUrl == targetUrl)
        {
            return;
        }

        var monikers = _monikerProvider.GetFileLevelMonikers(_errors, inclusionRoot);
        var sourceGitUrl = _contributionProvider.GetGitUrl(referencingFile).originalContentGitUrl;
        var item = new FileLinkItem
        {
            InclusionRoot = inclusionRoot,
            SourceUrl = sourceUrl,
            SourceMonikerGroup = monikers.MonikerGroup,
            TargetUrl = targetUrl,
            SourceGitUrl = sourceGitUrl,
            SourceLine = source is null ? 1 : source.Line,
        };

        Watcher.Write(() => _links.Value.TryAdd(item));
    }

    public object Build(PublishModel publishModel)
    {
        var publishFiles = publishModel.Files.Where(item => !item.HasError && item.SourceFile != null).Select(item => item.SourceFile).ToHashSet();
        var links = _links.Value.Where(x => publishFiles.Contains(x.InclusionRoot)).OrderBy(x => x).ToArray();

        return new { links };
    }
}
