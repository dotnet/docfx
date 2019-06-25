// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class LegacyVersionProvider
    {
        private readonly Dictionary<Func<string, bool>, string> _versionConfigs = new Dictionary<Func<string, bool>, string>();

        public LegacyVersionProvider(Docset docset)
        {
            foreach (var (key, monikerRange) in docset.Config.MonikerRange)
            {
                _versionConfigs.Add(GlobUtility.CreateGlobMatcher(key), monikerRange);
            }
            _versionConfigs.Reverse();
        }

        public string GetLegacyVersion(Document file)
        {
            foreach (var (glob, monikerRange) in _versionConfigs)
            {
                if (glob(file.FilePath))
                {
                    return monikerRange;
                }
            }
            return default;
        }
    }
}
