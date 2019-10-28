// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class FileLinkMapBuilder
    {
        private readonly MonikerProvider _monikerProvider;
        private readonly ErrorLog _errorLog;
        private readonly ListBuilder<FileLinkItem> _links = new ListBuilder<FileLinkItem>();

        public FileLinkMapBuilder(MonikerProvider monikerProvider, ErrorLog errorLog)
        {
            Debug.Assert(monikerProvider != null);
            _monikerProvider = monikerProvider;
            _errorLog = errorLog;
        }

        public void AddFileLink(Document file, string targetUrl)
        {
            Debug.Assert(file != null);

            if (string.IsNullOrEmpty(targetUrl) || file.SiteUrl == targetUrl)
            {
                return;
            }

            var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file);
            if (error != null)
            {
                _errorLog.Write(file, error);
            }

            _links.Add(new FileLinkItem()
            {
                SourceUrl = file.SiteUrl,
                SourceMonikerGroup = MonikerUtility.GetGroup(monikers),
                TargetUrl = targetUrl,
            });
        }

        public object Build()
            => new
            {
                // TODO: OrderBy is not stable across platform,
                // need a way to consistently apply this to all sort calls.
                // https://github.com/dotnet/corefx/issues/15825
                Links = _links.ToList()
                .OrderBy(x => x.SourceUrl, StringComparer.Ordinal)
                .ThenBy(x => x.TargetUrl, StringComparer.Ordinal)
                .ThenBy(x => x.SourceMonikerGroup, StringComparer.Ordinal),
            };
    }
}
