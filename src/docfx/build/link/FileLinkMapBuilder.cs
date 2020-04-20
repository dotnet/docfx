// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class FileLinkMapBuilder
    {
        private readonly ErrorLog _errorLog;
        private readonly MonikerProvider _monikerProvider;
        private readonly PublishModelBuilder _publishModelBuilder;
        private readonly ContributionProvider _contributionProvider;
        private readonly ConcurrentHashSet<FileLinkItem> _links = new ConcurrentHashSet<FileLinkItem>();

        public FileLinkMapBuilder(ErrorLog errorLog, MonikerProvider monikerProvider, PublishModelBuilder publishModelBuilder, ContributionProvider contributionProvider)
        {
            _errorLog = errorLog;
            _monikerProvider = monikerProvider;
            _publishModelBuilder = publishModelBuilder;
            _contributionProvider = contributionProvider;
        }

        public void AddFileLink(FilePath inclusionRoot, FilePath referecningFile, string sourceUrl, string targetUrl, SourceInfo? source)
        {
            if (string.IsNullOrEmpty(targetUrl) || sourceUrl == targetUrl)
            {
                return;
            }

            var (errors, monikers) = _monikerProvider.GetFileLevelMonikers(inclusionRoot);
            var sourceGitUrl = _contributionProvider.GetGitUrl(referecningFile).originalContentGitUrl;

            _errorLog.Write(errors);
            _links.TryAdd(new FileLinkItem(inclusionRoot, sourceUrl, MonikerUtility.GetGroup(monikers), targetUrl, sourceGitUrl, source is null ? 1 : source.Line));
        }

        public object Build()
        {
            return new
            {
                Links = _links
                        .Where(x => _publishModelBuilder.HasOutput(x.InclusionRoot))
                        .OrderBy(x => x)
                        .ToArray(),
            };
        }
    }
}
