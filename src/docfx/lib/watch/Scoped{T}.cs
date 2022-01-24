// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Microsoft.Docs.Build;

public class Scoped<T> where T : class
{
    private readonly Func<T> _valueFactory;
    private readonly ConditionalWeakTable<object, T> _values = new();

    public Scoped() => _valueFactory = () => Activator.CreateInstance<T>();

    public Scoped(Func<T> valueFactory) => _valueFactory = valueFactory;

    public T Value => _values.GetValue(Watcher.GetCurrentScope(), _ => _valueFactory());
}
