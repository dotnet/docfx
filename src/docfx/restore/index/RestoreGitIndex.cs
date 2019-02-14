// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class RestoreGitIndex
    {
        public int Line { get; set; }

        public string Commit { get; set; }

        public string Branch { get; set; }

        public DateTime Date { get; set; }

        public string InUse { get; set; }
    }
}
