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
        private readonly ConcurrentHashSet<FileLinkItem> _links = new ConcurrentHashSet<FileLinkItem>();

        public FileLinkMapBuilder(ErrorLog errorLog, MonikerProvider monikerProvider, PublishModelBuilder publishModelBuilder)
        {
            _errorLog = errorLog;
            _monikerProvider = monikerProvider;
            _publishModelBuilder = publishModelBuilder;
        }

        public void AddFileLink(FilePath inclusionRoot, FilePath referecningFile, string sourceUrl, string targetUrl, SourceInfo? source)
        {
            if (string.IsNullOrEmpty(targetUrl) || sourceUrl == targetUrl)
            {
                return;
            }

            var (errors, monikers) = _monikerProvider.GetFileLevelMonikers(inclusionRoot);
            _errorLog.Write(errors);
            _links.TryAdd(new FileLinkItem(inclusionRoot, referecningFile, sourceUrl, MonikerUtility.GetGroup(monikers), targetUrl, source is null ? 1 : source.Line));
        }

        public object Build(ContributionProvider contributionProvider)
        {
            return new
            {
                Links = _links
                        .Where(x => _publishModelBuilder.HasOutput(x.InclusionRoot))
                        .OrderBy(x => x)
                        .Select(x =>
                        {
                            var (_, originalContentGitUrl, _, _) = contributionProvider.GetGitUrls(x.ReferencingFile);
                            if (!string.IsNullOrEmpty(originalContentGitUrl))
                            {
                                x.SourceGitUrl = originalContentGitUrl;
                            }
                            return x;
                        }),
            };
        }
    }
}
