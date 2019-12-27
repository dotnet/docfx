// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    internal enum PackageFetchOptions
    {
        None = 0,
        FullHistory = 0b0001,
        Optional = 0b0010,
    }
}
