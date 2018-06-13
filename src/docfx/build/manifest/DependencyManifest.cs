// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class DependencyManifest
    {
        public string Source { get; set; }

        public DependencyManifestItem[] Dependencies { get; set; }
    }
}
