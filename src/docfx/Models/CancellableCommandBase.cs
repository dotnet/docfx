// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.InteropServices;
using Spectre.Console.Cli;

namespace Docfx;

public abstract class CancellableCommandBase<TSettings> : Command<TSettings>
     where TSettings : CommandSettings
{
    public abstract int Execute(CommandContext context, TSettings settings, CancellationToken cancellation);

    public sealed override int Execute(CommandContext context, TSettings settings)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

        var exitCode = Execute(context, settings, cancellationSource.Token);
        return exitCode;

        void onSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}
