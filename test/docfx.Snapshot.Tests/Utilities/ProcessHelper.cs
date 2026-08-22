// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Docfx.Tests;

internal static class ProcessHelper
{
    public static async ValueTask<int> ExecAsync(
        string filename,
        string args,
        string workingDirectory = null,
        KeyValuePair<string, string>[] environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(filename, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            ErrorDialog = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
        };

        if (workingDirectory != null)
            psi.WorkingDirectory = Path.GetFullPath(workingDirectory);

        if (environmentVariables != null)
        {
            foreach (var v in environmentVariables)
                psi.EnvironmentVariables.Add(v.Key, v.Value);
        }

        using var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived += OnErrorDataReceived;

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // On .NET 6 or later. Process.WaitForExitAsync wait for redirected output reads
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TerminateProcess(process);
            throw;
        }
        catch
        {
            TerminateProcess(process);
            return -1;
        }

        CleanupProcess(process);
        return process.ExitCode;
    }

    private static void CleanupProcess(Process process)
    {
        // Stop async event processing. (To avoid callback invoked after disposed)
        process.CancelOutputRead();
        process.CancelErrorRead();

        // Remove event handler
        process.OutputDataReceived -= OnOutputDataReceived;
        process.ErrorDataReceived -= OnErrorDataReceived;
    }

    private static void TerminateProcess(Process process)
    {
        CleanupProcess(process);

        if (process.HasExited)
            return;

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore exception
        }
    }

    private static void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        var message = e.Data;
        if (string.IsNullOrEmpty(message))
            return;

        // Ignore output message. When need to output logs. uncomment following line.
        // TestContext.Current.TestOutputHelper.WriteLine(message);
    }

    private static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        var message = e.Data;
        if (string.IsNullOrEmpty(message))
            return;

        // Ignore output message. When need to output logs. uncomment following line.
        // TestContext.Current.TestOutputHelper.WriteLine($"Error: {message}");
    }
}
