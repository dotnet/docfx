// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Cli;

namespace Docfx;

internal static class SpectreConsoleExtensions
{
    public static void WriteException(this IAnsiConsole ansiConsole, Exception e)
    {
        if (e is CommandAppException cae)
        {
            if (cae.Pretty is { } pretty)
                AnsiConsole.Write(pretty);
            else
                AnsiConsole.MarkupInterpolated($"[red]Error:[/] {e.Message}");
            return;
        }
        else
        {
            AnsiConsole.WriteException(e, new ExceptionSettings()
            {
                Format = ExceptionFormats.ShortenEverything,
                Style = new()
                {
                    ParameterName = Color.Grey,
                    ParameterType = Color.Grey78,
                    LineNumber = Color.Grey78,
                },
            });
        }
    }
}
