// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class FileLinkMapBuilder
    {
        private readonly MonikerProvider _monikerProvider;
        private readonly ListBuilder<FileLinkItem> _links = new ListBuilder<FileLinkItem>();

        public FileLinkMapBuilder(MonikerProvider monikerProvider)
        {
            Debug.Assert(monikerProvider != null);
            _monikerProvider = monikerProvider;
        }

        public void AddFileLink(string sourceUrl, Document file, string targetUrl)
        {
            Debug.Assert(sourceUrl != null);
            Debug.Assert(file != null);
            Debug.Assert(targetUrl != null);

            var (_, monikers) = _monikerProvider.GetFileLevelMonikers(file);
            _links.Add(new FileLinkItem()
            {
                SourceUrl = sourceUrl,
                SourceMonikerGroup = MonikerUtility.GetGroup(monikers),
                TargetUrl = targetUrl,
            });
        }

        public object Build()
            => new { Links = _links.ToList() };
    }
}
