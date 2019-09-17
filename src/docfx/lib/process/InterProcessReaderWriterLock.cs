// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public struct InterProcessReaderWriterLock : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly string _filePath;

        private InterProcessReaderWriterLock(FileStream fileStream, string filePath)
        {
            _fileStream = fileStream;
            _filePath = filePath;
        }

        public static InterProcessReaderWriterLock CreateReaderLock(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var filePath = Path.Combine(AppData.MutexRoot, HashUtility.GetMd5Hash(lockName));
            var fileLock = WaitFile(lockName, filePath, FileAccess.Read, FileShare.Read);

            return new InterProcessReaderWriterLock(fileLock, filePath);
        }

        public static InterProcessReaderWriterLock CreateWriterLock(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var filePath = Path.Combine(AppData.MutexRoot, HashUtility.GetMd5Hash(lockName));
            var fileLock = WaitFile(lockName, filePath, FileAccess.Write, FileShare.None);

            return new InterProcessReaderWriterLock(fileLock, filePath);
        }

        private static FileStream WaitFile(string name, string path, FileAccess access, FileShare fileShare)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start <= TimeSpan.FromMinutes(1))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (new GlobalMutex(path))
                    {
                        return new FileStream(path, FileMode.OpenOrCreate, access, fileShare);
                    }
                }
                catch
                {
                    if (DateTime.UtcNow - start > TimeSpan.FromSeconds(10))
                    {
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                        lock (Console.Out)
#pragma warning restore CA2002
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Waiting for another process to access '{name}'");
                            Console.ResetColor();
                        }
                    }

                    Thread.Sleep(200);
                    continue;
                }
            }

            throw new ApplicationException($"Failed to access resource {name}");
        }

        public void Dispose()
        {
            if (_fileStream != null)
            {
                using (new GlobalMutex(_filePath))
                {
                    _fileStream.Dispose();
                }
            }
        }

        private class GlobalMutex : IDisposable
        {
            private readonly Mutex _mutex;

            public GlobalMutex(string name)
            {
                _mutex = new Mutex(initiallyOwned: false, $"Global\\{HashUtility.GetMd5Hash(name)}");
                _mutex.WaitOne();
            }

            public void Dispose()
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }
    }
}
