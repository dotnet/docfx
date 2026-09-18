// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

#nullable enable

namespace Docfx;

internal sealed class DocfxHelpProvider : HelpProvider
{
    private readonly UsageStyle? _usageStyle;

    public DocfxHelpProvider(ICommandAppSettings settings)
        : base(settings)
    {
        _usageStyle = settings.HelpProviderStyles?.Usage;
    }

    public override IEnumerable<IRenderable> GetUsage(ICommandModel model, ICommandInfo? command)
    {
        if (command is not { IsDefaultCommand: true, Parent: null }
            || !model.Commands.Any(command => !command.IsHidden && !command.IsDefaultCommand))
        {
            return base.GetUsage(model, command);
        }

        var usage = new Paragraph()
            .Append("USAGE:", _usageStyle?.Header)
            .Append("\n    ")
            .Append(model.ApplicationName)
            .Append(" [COMMAND]", _usageStyle?.Command);

        foreach (var argument in command.Parameters.OfType<ICommandArgument>().Where(argument => argument.IsRequired).OrderBy(argument => argument.Position))
        {
            usage.Append($" <{argument.Value}>", _usageStyle?.RequiredArgument);
        }

        foreach (var argument in command.Parameters.OfType<ICommandArgument>().Where(argument => !argument.IsRequired).OrderBy(argument => argument.Position))
        {
            usage.Append($" [{argument.Value}]", _usageStyle?.OptionalArgument);
        }

        usage.Append(" [OPTIONS]\n", _usageStyle?.Options);
        return [usage];
    }
}
