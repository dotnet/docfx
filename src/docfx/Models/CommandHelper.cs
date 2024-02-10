// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.NetworkInformation;
using Docfx.Common;

#nullable enable

namespace Docfx;

internal class CommandHelper
{
    public static int Run(Action run)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        run();

        Logger.Flush();
        Logger.UnregisterAllListeners();

        return 0;
    }

    public static int Run(LogOptions options, Action run)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        if (!string.IsNullOrWhiteSpace(options.LogFilePath))
        {
            Logger.RegisterListener(new ReportLogListener(options.LogFilePath));
        }

        if (options.LogLevel.HasValue)
        {
            Logger.LogLevelThreshold = options.LogLevel.Value;
        }
        else if (options.Verbose)
        {
            Logger.LogLevelThreshold = LogLevel.Verbose;
        }

        Logger.WarningsAsErrors = options.WarningsAsErrors;

        run();

        Logger.Flush();
        Logger.UnregisterAllListeners();
        Logger.PrintSummary();

        return Logger.HasError ? -1 : 0;
    }

    public static bool IsTcpPortAlreadyUsed(string? host, int? port)
    {
        port ??= 8080;

        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var ipEndpoints = ipGlobalProperties.GetActiveTcpListeners()
                                            .Where(x => x.Port == port);

        if (!ipEndpoints.Any())
            return false; // Specified port is not used by any endpoint.

        switch (host)
        {
            case null:
            case "localhost":
                return ipEndpoints.Any(x => IPAddress.IsLoopback(x.Address)); // Check both IPv4/IPv6 loopback address.
            default:
                if (IPAddress.TryParse(host, out var address))
                {
                    return ipEndpoints.Any(x => x.Address == address);
                }
                else
                {
                    // Anything not recognized as a valid IP address (e.g. `*`) binds to all IPv4 and IPv6 IPAddresses.
                    return true;
                }
        }
    }
}
