// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Plugins;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Docfx.Common;

public sealed class ConsoleLogListener : ILoggerListener
{
    private const LogLevel LogLevelThreshold = LogLevel.Verbose;

    public void WriteLine(ILogItem item)
    {
        var level = item.LogLevel;
        if (level < LogLevelThreshold)
            return;

        var consoleColor = level switch
        {
            LogLevel.Verbose => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Suggestion => ConsoleColor.Blue,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
        };

        var message = new StringBuilder();
        if (!string.IsNullOrEmpty(item.File))
        {
            message.Append($"{Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, item.File))}");
            if (!string.IsNullOrEmpty(item.Line))
                message.Append($"({item.Line},1)");
            if (!string.IsNullOrEmpty(item.Code))
            {
                message.Append($": {level.ToString().ToLowerInvariant()} {item.Code}");
            }
            else
            {
                // Append warning/error message to distinguish log category from message.
                if (level >= LogLevel.Warning)
                    message.Append($": {level.ToString().ToLowerInvariant()}");
            }
            message.Append(": ");
        }
        else
        {
            if (level >= LogLevel.Warning)
                message.Append($"{level.ToString().ToLowerInvariant()}: ");
            if (!string.IsNullOrEmpty(item.Code))
                message.Append($"{item.Code}: ");
        }

        message.Append(item.Message);

        AnsiConsole.Console.Write(new NonBreakingText($"{message}\n", new(consoleColor)));
    }

    public void Dispose()
    {
    }

    public void Flush()
    {
    }

    class NonBreakingText(string text, Style style) : IRenderable
    {
        private readonly Paragraph _paragraph = new(text, style);

        public Measurement Measure(RenderOptions options, int maxWidth) => ((IRenderable)_paragraph).Measure(options, int.MaxValue);
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => ((IRenderable)_paragraph).Render(options, int.MaxValue);
    }
}
