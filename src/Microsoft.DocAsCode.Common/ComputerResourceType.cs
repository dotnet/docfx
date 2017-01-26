// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    [Flags]
    public enum ComputerResourceType
    {
        None = 0,
        Cpu = 1,
        DiskIO = 2,
        NetworkIO = 4,
    }
}
