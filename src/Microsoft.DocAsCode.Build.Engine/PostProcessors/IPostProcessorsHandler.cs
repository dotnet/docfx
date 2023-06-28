// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

internal interface IPostProcessorsHandler
{
    void Handle(List<PostProcessor> postProcessors, Manifest manifest, string outputFolder);
}
