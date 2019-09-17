// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public struct SharedAndExclusiveLock : IDisposable
    {
        private readonly FileStream _fileLock;
        private readonly string _lockName;

        public SharedAndExclusiveLock(string lockName, bool shared)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            _lockName = HashUtility.GetMd5Hash(lockName);
            var fileLockPath = Path.Combine(AppData.MutexRoot, _lockName);
            _fileLock = WaitFile(_lockName, fileLockPath, shared ? FileAccess.Read : FileAccess.Write, shared ? FileShare.Read : FileShare.None);
        }

        private static FileStream WaitFile(string name, string path, FileAccess access, FileShare fileShare)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"Try to access '{name}'");
                    Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(path)));

                    using (InterProcessMutex.Create(name))
                    {
                        return new FileStream(path, FileMode.OpenOrCreate, access, fileShare);
                    }
                }
                catch (Exception ex)
                {
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                    lock (Console.Out)
#pragma warning restore CA2002
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Waiting for another process to access '{name}'");
                        Console.ResetColor();

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                        Console.ResetColor();
                    }

                    Thread.Sleep(200);
                    continue;
                }
            }
        }

        public void Dispose()
        {
            if (_fileLock != null)
            {
                using (InterProcessMutex.Create(_lockName))
                {
                    _fileLock.Dispose();
                }
            }
        }
    }
}
