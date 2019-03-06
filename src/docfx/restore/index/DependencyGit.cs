// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class DependencyGit : DependencySlot
    {
        public string Commit { get; set; }

        public string Branch { get; set; }
    }
}
