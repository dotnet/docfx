// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal class PhaseProcessor
    {
        public List<IPhaseHandler> Handlers { get; } = new List<IPhaseHandler>();

        public void Process(List<HostService> hostServices, int maxParallelism)
        {
            foreach (var h in Handlers)
            {
                using (new LoggerPhaseScope(h.Name, LogLevel.Verbose))
                {
                    h.Handle(hostServices, maxParallelism);
                }
            }
        }
    }
}
