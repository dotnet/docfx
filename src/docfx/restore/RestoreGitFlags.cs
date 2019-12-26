// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    internal enum RestoreGitFlags
    {
        None = 0,
        DepthOne = 0b0001,
        Bare = 0b0010,
    }
}
