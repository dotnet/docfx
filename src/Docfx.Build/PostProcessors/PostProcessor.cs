// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

sealed class PostProcessor
{
    public string ContractName { get; set; }

    public IPostProcessor Processor { get; set; }
}
