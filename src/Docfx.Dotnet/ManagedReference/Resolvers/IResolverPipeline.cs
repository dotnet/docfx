// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal interface IResolverPipeline
{
    void Run(MetadataModel yaml, ResolverContext context);
}
