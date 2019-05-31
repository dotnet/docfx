// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal struct InterProcessMutex : IDisposable
    {
        private static readonly AsyncLocal<ImmutableStack<string>> t_mutexRecursionStack = new AsyncLocal<ImmutableStack<string>>();

        private Mutex _mutex;

        public static InterProcessMutex Create(string mutexName)
        {
            // avoid nested mutex with same mutex name
            t_mutexRecursionStack.Value = t_mutexRecursionStack.Value ?? ImmutableStack<string>.Empty;
            if (t_mutexRecursionStack.Value.Contains(mutexName))
            {
                throw new ApplicationException($"Nested mutex detected, mutex name: {mutexName}");
            }
            t_mutexRecursionStack.Value = t_mutexRecursionStack.Value.Push(mutexName);

            var mutex = new Mutex(initiallyOwned: false, $"Global\\{HashUtility.GetMd5Hash(mutexName)}");

            while (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
            {
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                lock (Console.Out)
#pragma warning restore CA2002
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Waiting for another process to access '{mutexName}'");
                    Console.ResetColor();
                }
            }

            return new InterProcessMutex { _mutex = mutex };
        }

        public void Dispose()
        {
            t_mutexRecursionStack.Value = t_mutexRecursionStack.Value.Pop();
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
