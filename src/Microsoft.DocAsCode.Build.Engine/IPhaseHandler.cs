// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    internal interface IPhaseHandler
    {
        void PreHandle(List<HostService> hostServices);
        void Handle(List<HostService> hostServices, int maxParallelism);
        void PostHandle(List<HostService> hostServices);
    }
}
