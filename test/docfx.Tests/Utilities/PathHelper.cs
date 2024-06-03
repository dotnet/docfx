// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Docfx.Tests;

internal class PathHelper
{
    public static string GetSolutionFolder([CallerFilePath] string callerFilePath = "")
    {
        if (callerFilePath.StartsWith("/_/"))
        {
            // PathMap is rewritten on CI environment (`ContinuousIntegrationBuild=true`).
            // So try to get workspace folder from GitHub Action environment variable.
            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (workspace != null)
                return workspace;
        }

        if (!File.Exists(callerFilePath))
        {
            // CallerFilePath is resolved at build timing.
            // If build/test is executed on separated machine. It failed to find file.
            throw new Exception($"File is not found. callerFilePath: {callerFilePath}");
        }

        return FindSolutionFolder(callerFilePath, "docfx");
    }

    /// <summary>
    /// Find docfx solution folder.
    /// </summary>
    private static string FindSolutionFolder(string callerFilePath, string solutionName)
    {
        var dir = new FileInfo(callerFilePath).Directory;
        while (dir != null
            && dir.Name != solutionName
            && !dir.EnumerateFiles($"{solutionName}.sln").Any())
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new Exception("Failed to find solution folder.");

        return dir.FullName;
    }
}
