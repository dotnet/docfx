// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.Docs.Build;

internal struct InterProcessMutex : IDisposable
{
    private static readonly AsyncLocal<ImmutableStack<string>> s_mutexRecursionStack = new();

    private Mutex _mutex;

    public static InterProcessMutex Create(string mutexName)
    {
        // avoid nested mutex with same mutex name
        var stack = s_mutexRecursionStack.Value ??= ImmutableStack<string>.Empty;
        if (stack.Contains(mutexName))
        {
            throw new InvalidOperationException($"Nested mutex detected, mutex name: {mutexName}");
        }
        s_mutexRecursionStack.Value = stack.Push(mutexName);

        var mutex = new Mutex(initiallyOwned: false, $"Global\\ipm-{HashUtility.GetSha256Hash(mutexName)}");

        try
        {
            while (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
            {
                Log.Important($"Waiting for another process to access '{mutexName}'", ConsoleColor.Yellow);
            }
        }
        catch (AbandonedMutexException)
        {
            // When another process/thread exited without releasing its mutex,
            // this exception is thrown and we've successfully acquired the mutex.
        }

        return new InterProcessMutex { _mutex = mutex };
    }

    public void Dispose()
    {
        s_mutexRecursionStack.Value = s_mutexRecursionStack.Value!.Pop();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
