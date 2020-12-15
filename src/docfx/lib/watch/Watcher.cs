// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public static class Watcher
    {
        private static readonly AsyncLocal<ImmutableStack<IFunction>> t_callstack = new();
        private static readonly AsyncLocal<int> t_activityId = new();

        public static T Read<T>(Func<T> valueFactory)
        {
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

        public static void StartActivity() => t_activityId.Value++;

        internal static int GetActivityId() => t_activityId.Value;

        internal static void BeginFunctionScope(IFunction function)
        {
            var stack = t_callstack.Value ?? ImmutableStack<IFunction>.Empty;

            t_callstack.Value = stack.Push(function);
        }

        internal static void EndFunctionScope(bool attachToParent = true)
        {
            var stack = t_callstack.Value;
            if (stack != null && !stack.IsEmpty)
            {
                t_callstack.Value = stack = stack.Pop(out var child);
                if (attachToParent && !stack.IsEmpty)
                {
                    stack.Peek().AddChild(child);
                }
            }
        }

        internal static void AttachToParent(IFunction child)
        {
            var stack = t_callstack.Value;
            if (stack != null && !stack.IsEmpty)
            {
                stack.Peek().AddChild(child);
            }
        }
    }
}
