// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ScopedErrorBuilder : ErrorBuilder
{
    private readonly AsyncLocal<ErrorBuilder?> _errors = new();

    private ErrorBuilder EnsureValue => _errors.Value ?? throw new InvalidOperationException();

    public IDisposable BeginScope(ErrorBuilder errors)
    {
        _errors.Value = errors;
        return new DelegatingDisposable(() => _errors.Value = null);
    }

    public override bool HasError => EnsureValue.HasError;

    public override bool FileHasError(FilePath file) => EnsureValue.FileHasError(file);

    public override void Add(Error error) => Watcher.Write(() => EnsureValue.Add(error));
}
