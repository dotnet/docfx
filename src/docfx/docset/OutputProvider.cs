// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class OutputProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<string>> _outputMapping = new ConcurrentDictionary<string, Lazy<string>>();

        public string GetOutputPath(Document file)
            => _outputMapping.GetOrAdd(file.FilePath, new Lazy<string>(() =>
            {
                var monikers = file.Docset.MonikersProvider.GetMonikers(file);
                if (monikers.Count == 0)
                {
                    return file.SitePath;
                }
                return Path.Combine(MonikerUtility.GetMonikersHash(monikers), file.SitePath);
            })).Value;
    }
}
