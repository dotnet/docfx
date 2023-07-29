// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace Docfx.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        RegisterNonPreviewSdkInstance();
    }

    /// <summary>
    /// Try to register non-preview version .NET SDK to avoid `NuGet.Frameworks` package related problems.
    /// </summary>
    /// <remarks>
    private static void RegisterNonPreviewSdkInstance()
    {
        if (MSBuildLocator.IsRegistered)
        {
            // `DotnetApiCatalog.Exec` is called before ModuleInitializer execution.
            MSBuildLocator.Unregister();
            Console.WriteLine($"Execute: MSBuildLocator.Unregister()");
        }

        // Gets non-preview .NET SDKs.
        var vsInstances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default)
                                        .Where(x => !x.MSBuildPath.Contains("-preview."));

        var vs = vsInstances.FirstOrDefault();
        if (vs == null)
            return;

        MSBuildLocator.RegisterInstance(vs);
        Console.WriteLine($"Using {vs.Name} {vs.Version}");
    }
}
