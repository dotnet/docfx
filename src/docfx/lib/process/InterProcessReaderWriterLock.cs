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
        private readonly string _lockName;

        private InterProcessReaderWriterLock(FileStream fileStream, string lockName)
        {
            _fileStream = fileStream;
            _lockName = lockName;
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

            return new InterProcessReaderWriterLock(fileLock, lockName);
        }

        private static FileStream WaitFile(string name, string path, FileAccess access, FileShare fileShare)
        {
            var start = DateTime.UtcNow;
            while (true)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (InterProcessMutex.Create(name))
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
        }

        public void Dispose()
        {
            if (_fileStream != null)
            {
                using (InterProcessMutex.Create(_lockName))
                {
                    _fileStream.Dispose();
                }
            }
        }
    }
}
