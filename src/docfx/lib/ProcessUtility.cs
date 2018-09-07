// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide process utility
    /// </summary>
    internal static class ProcessUtility
    {
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
        /// Create a file mutex to lock a resource/action
        /// </summary>
        /// <param name="mutexName">A globbaly unique mutext name</param>
        /// <param name="action">The action/resource you want to lock</param>
        public static async Task RunInsideMutex(string mutexName, Func<Task> action)
        {
            Debug.Assert(!string.IsNullOrEmpty(mutexName));

            Directory.CreateDirectory(AppData.MutexDir);

            var lockPath = Path.Combine(AppData.MutexDir, HashUtility.GetMd5Hash(mutexName));

            using (await AcquireFileMutex(mutexName, lockPath))
            {
                await action();
            }
        }

        /// <summary>
        /// Checks if the exception thrown by Process.Start is caused by file not found.
        /// </summary>
        public static bool IsNotFound(Win32Exception ex)
        {
            return ex.ErrorCode == -2147467259 || // Error_ENOENT = 0x1002D, No such file or directory
                   ex.ErrorCode == 2; // ERROR_FILE_NOT_FOUND = 0x2, The system cannot find the file specified
        }

        private static async Task<FileStream> AcquireFileMutex(string mutexName, string lockPath)
        {
            var retryDelay = 100;
            var lastWait = DateTime.UtcNow;

            while (true)
            {
                try
                {
                    return new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                }
                catch
                {
                    if (DateTime.UtcNow - lastWait > TimeSpan.FromSeconds(30))
                    {
                        lastWait = DateTime.UtcNow;
#pragma warning disable CA2002 // Do not lock on objects with weak identity
                        lock (Console.Out)
#pragma warning restore CA2002
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Waiting for another process to access '{mutexName}'");
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
