// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;

namespace Microsoft.Docs.Build;

internal class LanguageServerProgressReporter : IProgress<string>, IDisposable
{
    private readonly IWorkDoneObserver _workDoneManager;

    public LanguageServerProgressReporter(IWorkDoneObserver workDoneManager)
    {
        _workDoneManager = workDoneManager;
    }

    public void Dispose()
    {
        _workDoneManager.Dispose();
    }

    public void Report(string message)
    {
        _workDoneManager.OnNext(new WorkDoneProgressReport()
        {
            Message = message,
        });
    }
}
