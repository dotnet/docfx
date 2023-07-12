// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public static class BuildPhaseUtility
{
    public static void RunBuildSteps(IEnumerable<IDocumentBuildStep> buildSteps, Action<IDocumentBuildStep> action)
    {
        if (buildSteps != null)
        {
            foreach (var buildStep in buildSteps.OrderBy(step => step.BuildOrder))
            {
                action(buildStep);
            }
        }
    }
}
