// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class DependencyManifestItem
    {
        public string Source { get; }

        public DependencyType Type { get; }

        public DependencyManifestItem(string source, DependencyType type)
        {
            Source = source;
            Type = type;
        }
    }
}
