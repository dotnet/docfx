// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public struct InterProcessReaderWriterLock : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly string _lockHash;

        private InterProcessReaderWriterLock(FileStream fileStream, string lockHash)
        {
            _fileStream = fileStream;
            _lockHash = lockHash;
        }

        public static InterProcessReaderWriterLock CreateReaderLock(string lockName)
        {
            var lockHash = HashUtility.GetMd5Hash(lockName);
            var fileLock = WaitFile(lockName, lockHash, FileAccess.Read, FileShare.Read);

            return new InterProcessReaderWriterLock(fileLock, lockHash);
        }

        public static InterProcessReaderWriterLock CreateWriterLock(string lockName)
        {
            var lockHash = HashUtility.GetMd5Hash(lockName);
            var fileLock = WaitFile(lockName, lockHash, FileAccess.Write, FileShare.None);

            return new InterProcessReaderWriterLock(fileLock, lockHash);
        }

        private static FileStream WaitFile(string name, string lockHash, FileAccess access, FileShare fileShare)
        {
            var path = Path.Combine(AppData.MutexRoot, lockHash);
            var start = DateTime.UtcNow;

            while (true)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                    using (new GlobalMutex(lockHash))
                    {
                        return new FileStream(path, FileMode.OpenOrCreate, access, fileShare);
                    }
                }
                catch
                {
                    if (DateTime.UtcNow - start > TimeSpan.FromSeconds(30))
                    {
                        Log.Important($"Waiting for another process to access '{name}'", ConsoleColor.Yellow);
                    }

                    Thread.Sleep(200);
                }
            }

            throw new ApplicationException($"Failed to access resource {name}");
        }

        public void Dispose()
        {
            using (new GlobalMutex(_lockHash))
            {
                _fileStream.Dispose();
            }
        }

        private class GlobalMutex : IDisposable
        {
            private readonly Mutex _mutex;

            public GlobalMutex(string hash)
            {
                _mutex = new Mutex(initiallyOwned: false, $"Global\\iprwl-{hash}");

                try
                {
                    _mutex.WaitOne();
                }
                catch (AbandonedMutexException)
                {
                    // When another process/thread exited without releasing its mutex,
                    // this exception is thrown and we've successfully acquired the mutex.
                }
            }

            public void Dispose()
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}
