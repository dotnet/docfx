// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class FileLinkMapBuilder
    {
        private readonly ErrorLog _errorLog;
        private readonly MonikerProvider _monikerProvider;
        private readonly ContributionProvider _contributionProvider;
        private readonly ConcurrentHashSet<FileLinkItem> _links = new ConcurrentHashSet<FileLinkItem>();

        public FileLinkMapBuilder(ErrorLog errorLog, MonikerProvider monikerProvider, ContributionProvider contributionProvider)
        {
            _errorLog = errorLog;
            _monikerProvider = monikerProvider;
            _contributionProvider = contributionProvider;
        }

        public void AddFileLink(FilePath inclusionRoot, FilePath referencingFile, string sourceUrl, string targetUrl, SourceInfo? source)
        {
            if (string.IsNullOrEmpty(targetUrl) || sourceUrl == targetUrl)
            {
                return;
            }

            var (errors, monikers) = _monikerProvider.GetFileLevelMonikers(inclusionRoot);
            var sourceGitUrl = _contributionProvider.GetGitUrl(referencingFile).originalContentGitUrl;

            _errorLog.Write(errors);
            _links.TryAdd(new FileLinkItem(
                inclusionRoot, sourceUrl, monikers.MonikerGroup, targetUrl, sourceGitUrl, source is null ? 1 : source.Line));
        }

        public object Build(HashSet<FilePath> publishFiles)
        {
            return new
            {
                Links = _links
                        .Where(x => publishFiles.Contains(x.InclusionRoot) && !_errorLog.HasError(x.InclusionRoot))
                        .OrderBy(x => x)
                        .ToArray(),
            };
        }
    }
}
