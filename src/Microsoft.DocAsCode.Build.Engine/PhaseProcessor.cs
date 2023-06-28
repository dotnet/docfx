// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.Build.Engine;

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
