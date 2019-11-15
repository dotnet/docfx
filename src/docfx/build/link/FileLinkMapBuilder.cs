// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class FileLinkMapBuilder
    {
        private readonly Context _context;
        private readonly MonikerProvider _monikerProvider;
        private readonly ErrorLog _errorLog;
        private readonly ConcurrentHashSet<FileLinkItem> _links = new ConcurrentHashSet<FileLinkItem>();

        public FileLinkMapBuilder(MonikerProvider monikerProvider, ErrorLog errorLog, Context context)
        {
            Debug.Assert(monikerProvider != null);
            _monikerProvider = monikerProvider;
            _errorLog = errorLog;
            _context = context;
        }

        public void AddFileLink(Document file, string targetUrl)
        {
            Debug.Assert(file != null);

            if (string.IsNullOrEmpty(targetUrl) || file.SiteUrl == targetUrl)
            {
                return;
            }

            var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file.FilePath);
            if (error != null)
            {
                _errorLog.Write(file, error);
            }

            _links.TryAdd(new FileLinkItem()
            {
                SourceFile = file,
                SourceUrl = file.SiteUrl,
                SourceMonikerGroup = MonikerUtility.GetGroup(monikers),
                TargetUrl = targetUrl,
            });
        }

        public object Build() =>
            new
            {
                Links = _links
                .Where(x => _context.PublishModelBuilder.IsIncludedInOutput(x.SourceFile))
                .OrderBy(_ => _),
            };
    }
}
