// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class WriteFunction : IFunction
{
    private readonly Action _action;

    public WriteFunction(Action action) => _action = action;

    public void AddChild(IFunction childFunction) { }

    public bool HasChanged() => false;

    public void Replay() => _action();
}
