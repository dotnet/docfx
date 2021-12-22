// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.Docs.Build;

public static class Watcher
{
    private static readonly object s_defaultScope = new();
    private static readonly AsyncLocal<ImmutableStack<IFunction>> s_callstack = new();
    private static readonly AsyncLocal<IDisposable?> s_scope = new();

    public static T Read<T>(Func<T> valueFactory)
    {
        if (s_scope.Value is null)
        {
            return valueFactory();
        }

        var function = new ReadFunction<T>(valueFactory);
        BeginFunctionScope(function);

        try
        {
            var result = valueFactory();

            function.ChangeToken = result;
            return result;
        }
        finally
        {
            EndFunctionScope();
        }
    }

    public static T Read<T, TChangeToken>(Func<T> valueFactory, Func<TChangeToken> changeTokenFactory)
    {
        if (s_scope.Value is null)
        {
            return valueFactory();
        }

        var function = new ReadFunction<TChangeToken>(changeTokenFactory);
        BeginFunctionScope(function);

        try
        {
            var changeToken = changeTokenFactory();
            var result = valueFactory();

            function.ChangeToken = changeToken;
            return result;
        }
        finally
        {
            EndFunctionScope();
        }
    }

    public static void Write(Action action)
    {
        if (s_scope.Value is null)
        {
            action();
            return;
        }

        var function = new WriteFunction(action);
        BeginFunctionScope(function);

        try
        {
            action();
        }
        finally
        {
            EndFunctionScope();
        }
    }

    public static IDisposable BeginScope()
    {
        if (s_scope.Value != null)
        {
            throw new InvalidOperationException("Cannot start a nested scope.");
        }
        return s_scope.Value = new DelegatingDisposable(() => s_scope.Value = null);
    }

    internal static object GetCurrentScope() => s_scope.Value ?? s_defaultScope;

    internal static void BeginFunctionScope(IFunction function)
    {
        var stack = s_callstack.Value ?? ImmutableStack<IFunction>.Empty;

        s_callstack.Value = stack.Push(function);
    }

    internal static void EndFunctionScope(bool attachToParent = true)
    {
        var stack = s_callstack.Value;
        if (stack != null && !stack.IsEmpty)
        {
            s_callstack.Value = stack = stack.Pop(out var child);
            if (attachToParent && !stack.IsEmpty)
            {
                stack.Peek().AddChild(child);
            }
        }
    }

    internal static void AttachToParent(IFunction child)
    {
        var stack = s_callstack.Value;
        if (stack != null && !stack.IsEmpty)
        {
            stack.Peek().AddChild(child);
        }
    }
}
