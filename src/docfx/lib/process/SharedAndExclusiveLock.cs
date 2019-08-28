// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public struct SharedAndExclusiveLock : IDisposable
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private readonly bool _shared;
        private readonly string _acquirer;
        private readonly string _lockName;

        public SharedAndExclusiveLock(string lockName, bool shared, TimeSpan? timeout = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            _shared = shared;
            _lockName = lockName;
            bool acquired;
            var now = DateTime.UtcNow;
            do
            {
                (acquired, _acquirer) = shared ? ProcessUtility.AcquireSharedLock(lockName) : ProcessUtility.AcquireExclusiveLock(lockName);

                if (acquired)
                {
                    break;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(100));
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                lock (Console.Out)
#pragma warning restore CA2002
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Waiting for another process to access '{lockName}'");
                    Console.ResetColor();
                }
            }
            while (DateTime.UtcNow - now < (timeout ?? _defaultTimeout));
        }

        public void Dispose()
        {
            var released = _shared ? ProcessUtility.ReleaseSharedLock(_lockName, _acquirer) : ProcessUtility.ReleaseExclusiveLock(_lockName, _acquirer);

            Debug.Assert(released);
        }
    }
}
