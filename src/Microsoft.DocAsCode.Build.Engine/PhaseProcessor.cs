// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    internal class PhaseProcessor
    {
        public List<IPhaseHandler> Handlers { get; } = new List<IPhaseHandler>();

        public void Process(List<HostService> hostServices, int maxParallelism)
        {
            foreach (var h in Handlers)
            {
                h.PreHandle(hostServices);
                h.Handle(hostServices, maxParallelism);
                h.PostHandle(hostServices);
            }
        }
    }
}
