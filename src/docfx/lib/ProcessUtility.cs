// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide process utility
    /// </summary>
    internal static class ProcessUtility
    {
        private const int _defaultLockExpireTimeInSecond = 60 * 60 * 6; /*six hours*/
        private static AsyncLocal<HashSet<string>> t_innerCall = new AsyncLocal<HashSet<string>>();

        /// <summary>
        /// Acquire a shared lock for input lock name
        /// The returned `acquirer` are used for tracking the acquired lock, instead of thread info, since the thread info may change in asynchronous programming model
        /// </summary>
        public static async Task<(bool acquired, string acquirer)> AcquireSharedLock(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var acquired = false;
            var acquirer = (string)null;
            var lockPath = GetLockFilePath(lockName);

            await ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();
                lockInfo = FilterExpiredAcquirers(lockInfo);

                if (lockInfo.Type == LockType.Exclusive)
                {
                    return Task.FromResult(lockInfo);
                }

                acquired = true;
                acquirer = Guid.NewGuid().ToString();
                lockInfo.Type = LockType.Shared;
                lockInfo.AcquiredBy.Add(new Acquirer { Id = acquirer, Date = DateTime.UtcNow });
                return Task.FromResult(lockInfo);
            });

            return (acquired, acquirer);
        }

        public static async Task<bool> ReleaseSharedLock(string lockName, string acquirer)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));
            Debug.Assert(!string.IsNullOrEmpty(acquirer));

            var released = false;
            var lockPath = GetLockFilePath(lockName);

            await ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();

                if (lockInfo.Type != LockType.Shared)
                {
                    return Task.FromResult(lockInfo);
                }

                var removed = lockInfo.AcquiredBy.RemoveAll(i => i.Id == acquirer);
                Debug.Assert(removed <= 1);

                if (removed <= 0)
                {
                    return Task.FromResult(lockInfo);
                }

                if (!lockInfo.AcquiredBy.Any())
                {
                    lockInfo.Type = LockType.None;
                }

                released = true;
                return Task.FromResult(lockInfo);
            });

            return released;
        }

        /// <summary>
        /// Acquire a exclusive lock for input lock name
        /// The returned `acquirer` are used for tracking the acquired lock, instead of thread info, since the thread info may change in asynchronous programming model
        /// </summary>
        public static async Task<(bool acquired, string acquirer)> AcquireExclusiveLock(string lockName)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));

            var acquired = false;
            var acquirer = (string)null;
            var lockPath = GetLockFilePath(lockName);

            await ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();
                lockInfo = FilterExpiredAcquirers(lockInfo);

                if (lockInfo.Type != LockType.None)
                {
                    return Task.FromResult(lockInfo);
                }

                acquirer = Guid.NewGuid().ToString();
                acquired = true;
                lockInfo.Type = LockType.Exclusive;
                lockInfo.AcquiredBy = new List<Acquirer> { new Acquirer { Id = acquirer, Date = DateTime.UtcNow } };
                return Task.FromResult(lockInfo);
            });

            return (acquired, acquirer);
        }

        public static async Task<bool> ReleaseExclusiveLock(string lockName, string acquirer)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockName));
            Debug.Assert(!string.IsNullOrEmpty(acquirer));

            var released = false;
            var lockPath = GetLockFilePath(lockName);

            await ReadAndWriteFile<LockInfo>(lockPath, lockInfo =>
            {
                lockInfo = lockInfo ?? new LockInfo();

                if (lockInfo.Type != LockType.Exclusive)
                {
                    return Task.FromResult(lockInfo);
                }

                Debug.Assert(lockInfo.AcquiredBy.Count == 1);
                var removed = lockInfo.AcquiredBy.RemoveAll(i => i.Id == acquirer);
                Debug.Assert(removed <= 1);

                if (removed <= 0)
                {
                    return Task.FromResult(lockInfo);
                }

                if (!lockInfo.AcquiredBy.Any())
                {
                    lockInfo.Type = LockType.None;
                }

                released = true;
                return Task.FromResult(lockInfo);
            });

            return released;
        }

        /// <summary>
        /// Start a new process and wait for its execution asynchroniously
        /// </summary>
        public static Task<(string stdout, string stderr)> Execute(
            string fileName, string commandLineArgs, string cwd = null, bool stdout = true, bool stderr = true)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName));

            var tcs = new TaskCompletionSource<(string, string)>();

            var error = new StringBuilder();
            var output = new StringBuilder();
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = cwd,
                Arguments = commandLineArgs,
                UseShellExecute = false,
                RedirectStandardOutput = stdout,
                RedirectStandardError = stderr,
            };

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = psi,
            };

            if (stdout)
            {
                process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
            }
            if (stderr)
            {
                process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);
            }

            var processExited = new object();
            process.Exited += (a, b) =>
            {
                lock (processExited)
                {
                    // Wait for exit here to ensure the standard output/error is flushed.
                    process.WaitForExit();
                }

                if (process.ExitCode == 0)
                {
                    tcs.TrySetResult((output.ToString().Trim(), error.ToString().Trim()));
                }
                else
                {
                    var message = $"'\"{fileName}\" {commandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: \nSTDOUT:'{output}'\nSTDERR:\n'{error}'";

                    tcs.TrySetException(new InvalidOperationException(message));
                }
            };

            lock (processExited)
            {
                process.Start();

                if (stdout)
                {
                    // Thread.Sleep(10000);
                    // BeginOutputReadLine() and Exited event handler may have competition issue, above code can easily reproduce this problem
                    // Add lock to ensure the locked area code can be always exected before exited event
                    process.BeginOutputReadLine();
                }
                if (stderr)
                {
                    process.BeginErrorReadLine();
                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Reads the content of a file, update content and write back to file in one atomic operation
        /// </summary>
        public static async Task ReadAndWriteFile<T>(string path, Func<T, Task<T>> update)
        {
            await RunInsideMutex(path, async () =>
            {
                using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    var streamReader = new StreamReader(file);
                    var result = JsonUtility.Deserialize<T>(streamReader.ReadToEnd());

                    file.Position = 0;
                    var updatedResult = await update(result);
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
        public static async Task<string> ReadFile(string path)
        {
            string result = null;
            await RunInsideMutex(path, () =>
            {
                result = File.ReadAllText(path);
                return Task.CompletedTask;
            });
            return result;
        }

        public static async Task<T> ReadFile<T>(string path, Func<Stream, T> read)
        {
            T result = default;
            await RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
                {
                    result = read(fs);
                    return Task.CompletedTask;
                }
            });
            return result;
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="ReadFile(string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static Task WriteFile(string path, string content)
        {
            return RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write(content);
                }
                return Task.CompletedTask;
            });
        }

        public static Task WriteFile(string path, Action<Stream> write)
        {
            return RunInsideMutex(path, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                {
                    write(fs);
                }
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Create a file mutex to lock a resource/action
        /// </summary>
        /// <param name="mutexName">A globbaly unique mutext name</param>
        /// <param name="action">The action/resource you want to lock</param>
        public static async Task RunInsideMutex(string mutexName, Func<Task> action)
        {
            Debug.Assert(!string.IsNullOrEmpty(mutexName));

            var lockPath = Path.Combine(AppData.MutexRoot, HashUtility.GetMd5Hash(mutexName));

            // avoid the RunInsideMutex to be nested used
            // doesn't support to require a lock before releasing a lock
            // which may cause deadlock
            t_innerCall.Value = t_innerCall.Value ?? new HashSet<string>();
            if (!t_innerCall.Value.Add(lockPath))
            {
                throw new NotImplementedException($"Nested call to RunInsideMutex is not detected, {mutexName}");
            }

            try
            {
                Directory.CreateDirectory(AppData.MutexRoot);

                using (await RetryUntilSucceed(mutexName, IsFileAlreadyExistsException, CreateFile))
                {
                    await action();
                }

                FileStream CreateFile() => new FileStream(
                    lockPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.DeleteOnClose);
            }
            finally
            {
                t_innerCall.Value.Remove(lockPath);
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

        /// <summary>
        /// Checks if the exception thrown by new FileStream is caused by another process holding the file lock.
        /// </summary>
        public static bool IsFileAlreadyExistsException(Exception ex)
        {
            if (ex is IOException ioe)
            {
                return ex.HResult == 17 // Mac
                    || ex.HResult == -2147024816; // Windows
            }
            return ex is UnauthorizedAccessException;
        }

        private static async Task<T> RetryUntilSucceed<T>(string name, Func<Exception, bool> expectException, Func<T> action)
        {
            var retryDelay = 100;
            var lastWait = DateTime.UtcNow;

            while (true)
            {
                try
                {
                    return action();
                }
                catch (Exception ex) when (expectException(ex))
                {
                    if (DateTime.UtcNow - lastWait > TimeSpan.FromSeconds(30))
                    {
                        lastWait = DateTime.UtcNow;
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                        lock (Console.Out)
#pragma warning restore CA2002
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Waiting for another process to access '{name}'");
                            Console.ResetColor();
                        }
                    }

                    await Task.Delay(retryDelay);
                    retryDelay = Math.Min(retryDelay + 100, 1000);
                }
            }
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

            lockInfo.AcquiredBy.RemoveAll(r => DateTime.UtcNow - r.Date > TimeSpan.FromSeconds(_defaultLockExpireTimeInSecond));

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
