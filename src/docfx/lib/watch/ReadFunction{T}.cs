// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ReadFunction<T> : IFunction
{
    private readonly Func<T> _changeTokenFactory;

    internal T? ChangeToken { get; set; }

    public ReadFunction(Func<T> changeTokenFactory) => _changeTokenFactory = changeTokenFactory;

    public bool HasChanged() => !Equals(ChangeToken, _changeTokenFactory());

    public void AddChild(IFunction childFunction) { }

    public void Replay() { }
}
