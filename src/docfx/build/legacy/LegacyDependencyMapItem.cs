// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class LegacyDependencyMapItem
    {
        public string From { get; }

        public string To { get; }

        public string Version { get; }

        public LegacyDependencyMapType Type { get; }

        public LegacyDependencyMapItem(string from, string to, string version, LegacyDependencyMapType type)
        {
            From = from;
            To = to;
            Version = version;
            Type = type;
        }
    }
}
