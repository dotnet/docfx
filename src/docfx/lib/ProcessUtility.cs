// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        private static AsyncLocal<object> t_innerCall = new AsyncLocal<object>();

        /// <summary>
        /// Start a new process and wait for its execution asynchroniously
        /// </summary>
        public static Task<string> Execute(string fileName, string commandLineArgs, string cwd = null, bool redirectOutput = true)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName));

            var tcs = new TaskCompletionSource<string>();

            var error = new StringBuilder();
            var output = new StringBuilder();
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = cwd,
                Arguments = commandLineArgs,
                UseShellExecute = false,
            };

            if (redirectOutput)
            {
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
            }

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = psi,
            };

            if (redirectOutput)
            {
                process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
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
                    tcs.TrySetResult(output.ToString().Trim());
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

                if (redirectOutput)
                {
                    // Thread.Sleep(10000);
                    // BeginOutputReadLine() and Exited event handler may have competition issue, above code can easily reproduce this problem
                    // Add lock to ensure the locked area code can be always exected before exited event
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="WriteFile(string,string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static Task<string> ReadFile(string path)
        {
            return RetryUntilSucceed(path, IsFileUsedByAnotherProcessException, () =>
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
                using (var reader = new StreamReader(fs))
                {
                    return reader.ReadToEnd();
                }
            });
        }

        public static Task<T> ReadFile<T>(string path, Func<Stream, T> read)
        {
            return RetryUntilSucceed(path, IsFileUsedByAnotherProcessException, () =>
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
                {
                    return read(fs);
                }
            });
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="ReadFile(string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static Task WriteFile(string path, string content)
        {
            return RetryUntilSucceed(path, IsFileUsedByAnotherProcessException, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write(content);
                }
                return 0;
            });
        }

        public static Task WriteFile(string path, Action<Stream> write)
        {
            return RetryUntilSucceed(path, IsFileUsedByAnotherProcessException, () =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
                {
                    write(fs);
                }
                return 0;
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

            // avoid the RunInsideMutex to be nested used
            // doesn't support to require a lock before releasing a lock
            // which may cause deadlock
            if (t_innerCall.Value != null)
            {
                throw new NotImplementedException("Nested call to RunInsideMutex is not supported yet");
            }

            t_innerCall.Value = new object();

            try
            {
                PathUtility.CreateDirectoryIfNotEmpty(AppData.MutexDir);

                var lockPath = Path.Combine(AppData.MutexDir, HashUtility.GetMd5Hash(mutexName));

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
                t_innerCall.Value = null;
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
        public static bool IsFileUsedByAnotherProcessException(Exception ex)
        {
            return ex is IOException ioe && (
                   ioe.HResult == 35 // Mac
                || ioe.HResult == 11 // Linux
                || ioe.HResult == -2147024864); // Windows
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
    }
}
