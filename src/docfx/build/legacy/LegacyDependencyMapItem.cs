// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class LegacyDependencyMapItem
    {
        public string From { get; set; }

        public string To { get; set; }

        public LegacyDependencyMapType Type { get; set; }
    }
}
