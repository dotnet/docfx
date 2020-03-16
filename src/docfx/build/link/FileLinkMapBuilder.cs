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

        public void AddFileLink(FilePath file, string sourceUrl, string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl) || sourceUrl == targetUrl)
            {
                return;
            }

            var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file);
            if (error != null)
            {
                _errorLog.Write(error);
            }

            _links.TryAdd(new FileLinkItem(file, sourceUrl, MonikerUtility.GetGroup(monikers), targetUrl));
        }

        public object Build()
        {
            return new
            {
                Links = from link in _links where _publishModelBuilder.HasOutput(link.SourceFile) orderby link select link,
            };
    }
}
}
