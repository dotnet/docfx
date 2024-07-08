// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Docfx.Common;
using Docfx.Exceptions;

#nullable enable

namespace Docfx;

internal static class PlaywrightHelper
{
    public static void EnsurePlaywrightNodeJsPath()
    {
        // Skip if playwright environment variable exists.
        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH") != null)
            return;

        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH") != null)
            return;

        if (!TryFindNodeExecutable(out var exePath, out var nodeVersion))
        {
            throw new DocfxException("Node.js executable is not found. Try to install Node.js or set the `PLAYWRIGHT_NODEJS_PATH` environment variable.");
        }

        Logger.LogInfo($"Using Node.js {nodeVersion} executable.");
        Logger.LogVerbose($"Path: {exePath}");

        Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", exePath, EnvironmentVariableTarget.Process);
    }

    private static bool TryFindNodeExecutable(out string exePath, out string nodeVersion)
    {
        // Find Node.js executable installation path from PATHs.
        string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "node.exe" : "node";

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null)
            throw new DocfxException("Failed to get `PATH` environment variable.");

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            string fullPath = Path.GetFullPath(Path.Combine(path, exeName));

            if (File.Exists(fullPath))
            {
                exePath = fullPath;
                nodeVersion = GetNodeVersion(exePath);
                return true;
            }
        }

        exePath = "";
        nodeVersion = "";
        return false;
    }

    /// <summary>
    /// Returns `node --version` command result
    /// </summary>
    private static string GetNodeVersion(string exePath)
    {
        using var memoryStream = new MemoryStream();
        using var stdoutWriter = new StreamWriter(memoryStream);

        var exitCode = CommandUtility.RunCommand(new CommandInfo { Name = exePath, Arguments = "--version" }, stdoutWriter);

        if (exitCode != 0)
            return "";

        stdoutWriter.Flush();
        memoryStream.Position = 0;

        using var streamReader = new StreamReader(memoryStream);
        return streamReader.ReadLine() ?? "";
    }
}
