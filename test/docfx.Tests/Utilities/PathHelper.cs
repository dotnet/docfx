// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Docfx.Tests;

internal class PathHelper
{
    public static string GetSolutionFolder([CallerFilePath] string callerFilePath = "")
    {
        callerFilePath = NormalizeCallerFilePath(callerFilePath);

        if (!File.Exists(callerFilePath))
        {
            // CallerFilePath is resolved at build timing.
            // If build/test is executed on separated machine. It failed to find file.
            throw new FileNotFoundException($"File is not found. path: {callerFilePath}");
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

    /// <summary>
    /// Find TestData from callerFilePath.
    /// </summary>
    public static string ResolveTestDataPath(string path = "", [CallerFilePath] string callerFilePath = "")
    {
        if (Path.IsPathFullyQualified(path))
            return path;

        var dir = GetTestDataDirectory(callerFilePath);

        var resultPath = Path.Combine(dir, path);
        if (!File.Exists(resultPath) && !Directory.Exists(resultPath))
        {
            throw new FileNotFoundException($"Specified TestData file/directory is not found. path: {resultPath}");
        }

        return Path.GetFullPath(resultPath);
    }

    /// <summary>
    /// Find TestData from callerFilePath.
    /// </summary>
    public static string GetTestDataDirectory([CallerFilePath] string callerFilePath = "")
    {
        callerFilePath = NormalizeCallerFilePath(callerFilePath);

        if (!File.Exists(callerFilePath))
        {
            // CallerFilePath is resolved at build timing.
            // If build/test is executed on separated machine. It failed to find file.
            throw new FileNotFoundException($"File is not found. path: {callerFilePath}");
        }

        // Find closest `TestData` directory.
        var dir = new FileInfo(callerFilePath).Directory;
        while (dir != null)
        {
            var testDataDir = dir.EnumerateDirectories()
                                 .FirstOrDefault(d => d.Name == "TestData");
            if (testDataDir != null)
            {
                dir = testDataDir;
                break;
            }

            dir = dir.Parent;
        }

        if (dir == null)
            throw new DirectoryNotFoundException("Failed to find TestData folder.");

        return dir.FullName;
    }

    private static string NormalizeCallerFilePath(string callerFilePath)
    {
        // PathMap is rewritten on CI environment (`ContinuousIntegrationBuild=true`).
        // So try to get workspace folder from GitHub Action environment variable.
        if (callerFilePath.StartsWith("/_/"))
        {
            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (workspace != null)
                return workspace;
        }

        // Rewrite path when test runnign on WSL environment that are executed by Visual Studio Remote Testing.
        if (Environment.GetEnvironmentVariable("WSLENV") != null && callerFilePath.Contains('\\'))
        {
            var match = Regex.Match(callerFilePath, @"^([a-zA-Z]):\\(.+)$");
            if (match.Success)
            {
                var driveLetter = match.Groups[1].Value.ToLowerInvariant();
                var path = match.Groups[2].Value.Replace('\\', '/');
                return $"/mnt/{driveLetter}/{path}";
            }
        }

        return callerFilePath;
    }
}
