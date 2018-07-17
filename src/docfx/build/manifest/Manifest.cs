// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class Manifest
    {
        public string Repo { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }

        public FileManifest[] Files { get; set; }

        public Dictionary<string, DependencyManifestItem[]> Dependencies { get; set; }
    }
}
