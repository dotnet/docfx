// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

internal interface IPhaseHandler
{
    void Handle(List<HostService> hostServices, int maxParallelism);

    string Name { get; }

    BuildPhase Phase { get; }
}
