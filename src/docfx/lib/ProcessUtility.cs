// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide process utility
    /// </summary>
    internal static class ProcessUtility
    {
        private static readonly TimeSpan s_defaultLockExpireTime = TimeSpan.FromHours(6);
        private static readonly AsyncLocal<ImmutableStack<string>> t_mutexRecursionStack = new AsyncLocal<ImmutableStack<string>>();

        public static bool IsExclusiveLockHeld(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var lockPath = GetLockFilePath(lockName);
            var held = false;
            ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();
                lockInfo = FilterExpiredAcquirers(lockInfo);

                held = lockInfo.Type == LockType.Exclusive;

                return lockInfo;
            });

            return held;
        }

        /// <summary>
        /// Acquire a shared lock for input lock name
        /// The returned `acquirer` are used for tracking the acquired lock, instead of thread info, since the thread info may change in asynchronous programming model
        /// </summary>
        public static (bool acquired, string acquirer) AcquireSharedLock(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var acquired = false;
            var acquirer = (string)null;
            var lockPath = GetLockFilePath(lockName);

            ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();
                lockInfo = FilterExpiredAcquirers(lockInfo);

                if (lockInfo.Type == LockType.Exclusive)
                {
                    return lockInfo;
                }

                acquired = true;
                acquirer = Guid.NewGuid().ToString();
                lockInfo.Type = LockType.Shared;
                lockInfo.AcquiredBy.Add(new Acquirer { Id = acquirer, Date = DateTime.UtcNow });
                return lockInfo;
            });

            return (acquired, acquirer);
        }

        public static bool ReleaseSharedLock(string lockName, string acquirer)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));
            Debug.Assert(!string.IsNullOrEmpty(acquirer));

            var released = false;
            var lockPath = GetLockFilePath(lockName);

            ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();

                if (lockInfo.Type != LockType.Shared)
                {
                    return lockInfo;
                }

                var removed = lockInfo.AcquiredBy.RemoveAll(i => i.Id == acquirer);
                Debug.Assert(removed <= 1);

                if (removed <= 0)
                {
                    return lockInfo;
                }

                if (!lockInfo.AcquiredBy.Any())
                {
                    lockInfo.Type = LockType.None;
                }

                released = true;
                return lockInfo;
            });

            return released;
        }

        /// <summary>
        /// Acquire a exclusive lock for input lock name
        /// The returned `acquirer` are used for tracking the acquired lock, instead of thread info, since the thread info may change in asynchronous programming model
        /// </summary>
        public static (bool acquired, string acquirer) AcquireExclusiveLock(string lockName, bool force = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var acquired = false;
            var acquirer = (string)null;
            var lockPath = GetLockFilePath(lockName);
            if (force)
                File.Delete(lockPath);

            ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();
                lockInfo = FilterExpiredAcquirers(lockInfo);

                if (lockInfo.Type != LockType.None)
                {
                    return lockInfo;
                }

                acquirer = Guid.NewGuid().ToString();
                acquired = true;
                lockInfo.Type = LockType.Exclusive;
                lockInfo.AcquiredBy = new List<Acquirer> { new Acquirer { Id = acquirer, Date = DateTime.UtcNow } };
                return lockInfo;
            });

            return (acquired, acquirer);
        }

        public static bool ReleaseExclusiveLock(string lockName, string acquirer)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));
            Debug.Assert(!string.IsNullOrEmpty(acquirer));

            var released = false;
            var lockPath = GetLockFilePath(lockName);

            ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();

                if (lockInfo.Type != LockType.Exclusive)
                {
                    return lockInfo;
                }

                Debug.Assert(lockInfo.AcquiredBy.Count == 1);
                var removed = lockInfo.AcquiredBy.RemoveAll(i => i.Id == acquirer);
                Debug.Assert(removed <= 1);

                if (removed <= 0)
                {
                    return lockInfo;
                }

                if (!lockInfo.AcquiredBy.Any())
                {
                    lockInfo.Type = LockType.None;
                }

                released = true;
                return lockInfo;
            });

            return released;
        }

        /// <summary>
        /// Start a new process and wait for its execution to complete
        /// </summary>
        public static string Execute(string fileName, string commandLineArgs, string cwd = null, bool stdout = true)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = cwd,
                Arguments = commandLineArgs,
                UseShellExecute = false,
                RedirectStandardOutput = stdout,
                RedirectStandardError = true,
            };

            var process = Process.Start(psi);
            var result = stdout ? process.StandardOutput.ReadToEnd() : null;
            Log.Write(process.StandardError.ReadToEnd(), ConsoleColor.DarkRed);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"'\"{fileName}\" {commandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: \nSTDOUT:'{result}'");
            }

            return result;
        }

        /// <summary>
        /// Reads the content of a file, update content and write back to file in one atomic operation
        /// </summary>
        public static void ReadAndWriteFile<T>(string path, Func<T, T> update)
        {
            RunInsideMutex(path, () =>
            {
                using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    var streamReader = new StreamReader(file);
                    var result = JsonUtility.Deserialize<T>(streamReader.ReadToEnd(), path);

                    file.SetLength(0);
                    var updatedResult = update(result);
                    var steamWriter = new StreamWriter(file);
                    steamWriter.Write(JsonUtility.Serialize(updatedResult));
                    steamWriter.Close();
                }
            });
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="WriteFile(string,string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static string ReadFile(string path)
        {
            string result = null;
            RunInsideMutex(path, () =>
            {
                result = File.ReadAllText(path);
            });
            return result;
        }

        public static T ReadFile<T>(string path, Func<Stream, T> read)
        {
            T result = default;
            RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
                {
                    result = read(fs);
                }
            });
            return result;
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="ReadFile(string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static void WriteFile(string path, string content)
        {
            RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write(content);
                }
            });
        }

        public static void WriteFile(string path, Action<Stream> write)
        {
            RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                {
                    write(fs);
                }
            });
        }

        /// <summary>
        /// Create a file mutex to lock a resource/action
        /// </summary>
        /// <param name="mutexName">A globbaly unique mutext name</param>
        /// <param name="action">The action/resource you want to lock</param>
        public static void RunInsideMutex(string mutexName, Action action)
        {
            using (var mutex = new Mutex(initiallyOwned: false, $"Global\\{HashUtility.GetMd5Hash(mutexName)}"))
            {
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

                // avoid nested calls to RunInsideMutex with same mutex name
                t_mutexRecursionStack.Value = t_mutexRecursionStack.Value ?? ImmutableStack<string>.Empty;
                if (t_mutexRecursionStack.Value.Contains(mutexName))
                {
                    throw new ApplicationException($"Nested call to RunInsideMutex is detected, mutex name: {mutexName}");
                }
                t_mutexRecursionStack.Value = t_mutexRecursionStack.Value.Push(mutexName);

                try
                {
                    action();
                }
                finally
                {
                    t_mutexRecursionStack.Value = t_mutexRecursionStack.Value.Pop();
                    mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Checks if the exception thrown by Process.Start is caused by file not found.
        /// </summary>
        public static bool IsExeNotFoundException(Win32Exception ex)
        {
            return ex.ErrorCode == -2147467259 // Error_ENOENT = 0x1002D, No such file or directory
                || ex.ErrorCode == 2; // ERROR_FILE_NOT_FOUND = 0x2, The system cannot find the file specified
        }

        private static string GetLockFilePath(string lockName)
        {
            Directory.CreateDirectory(AppData.MutexRoot);
            var lockPath = Path.Combine(AppData.MutexRoot, HashUtility.GetMd5Hash(lockName) + "-rw");
            return lockPath;
        }

        private static LockInfo FilterExpiredAcquirers(LockInfo lockInfo)
        {
            Debug.Assert(lockInfo != null);

            lockInfo.AcquiredBy.RemoveAll(r => DateTime.UtcNow - r.Date > s_defaultLockExpireTime);

            if (!lockInfo.AcquiredBy.Any())
            {
                lockInfo.Type = LockType.None;
            }

            return lockInfo;
        }

        private enum LockType
        {
            None,
            Shared,
            Exclusive,
        }

        private class LockInfo
        {
            public LockType Type { get; set; }

            public List<Acquirer> AcquiredBy { get; set; } = new List<Acquirer>();
        }

        private class Acquirer
        {
            public string Id { get; set; }

            public DateTime Date { get; set; }
        }
    }
}
