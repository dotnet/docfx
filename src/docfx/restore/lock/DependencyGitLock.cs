// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    public class DependencyGitLock
    {
        public string Url { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }
    }
}
