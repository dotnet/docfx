// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ScopedProgressReporter : IProgress<string>
{
    private readonly AsyncLocal<IProgress<string>?> _progressReporter = new();

    private IProgress<string> EnsureValue => _progressReporter.Value ?? throw new InvalidOperationException();

    public IDisposable BeginScope(IProgress<string> progressReporter)
    {
        _progressReporter.Value = progressReporter;
        return new DelegatingDisposable(() => _progressReporter.Value = null);
    }

    public void Report(string value) => EnsureValue.Report(value);
}
