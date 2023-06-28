// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

internal sealed class PostProcessor
{
    public string ContractName { get; set; }

    public IPostProcessor Processor { get; set; }
}
