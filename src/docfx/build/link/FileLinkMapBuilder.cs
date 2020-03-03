// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;

#nullable enable

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

        public void AddFileLink(Document file, string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl) || file.SiteUrl == targetUrl)
            {
                return;
            }

            var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file.FilePath);
            if (error != null)
            {
                _errorLog.Write(error);
            }

            _links.TryAdd(new FileLinkItem(file, file.SiteUrl, MonikerUtility.GetGroup(monikers), targetUrl));
        }

        public object Build() =>
            new
            {
                Links = _links
                .Where(x => _publishModelBuilder.IsIncludedInOutput(x.SourceFile))
                .OrderBy(_ => _),
            };
    }
}
