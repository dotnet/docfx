// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class PublishManifestItem
    {
        public string Url { get; set; }

        public string Path { get; set; }

        public List<string> Monikers { get; set; }
    }
}
