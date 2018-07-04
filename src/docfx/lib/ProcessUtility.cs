// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private static readonly string s_lockDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "lock");

        /// <summary>
        /// Execute process with args
        /// </summary>
        /// <param name="fileName">The process path name or location</param>
        /// <param name="commandLineArgs">The process command line args</param>
        /// <param name="cwd">The current working directory</param>
        /// <param name="timeout">The timeout setting, default is none</param>
        /// <param name="outputHandler">The process output action</param>
        /// <returns>The executed result</returns>
        public static Task<string> Execute(string fileName, string commandLineArgs, string cwd = null, TimeSpan? timeout = null, Action<string, bool> outputHandler = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName));

            var tcs = new TaskCompletionSource<string>();

            var error = new StringBuilder();
            var output = new StringBuilder();

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = cwd,
                    Arguments = commandLineArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            // Todo: output steam to current window
            process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
            process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

            if (outputHandler != null)
            {
                process.OutputDataReceived += (sender, e) => outputHandler(e.Data, false);
                process.ErrorDataReceived += (sender, e) => outputHandler(e.Data, true);
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

            if (timeout != null)
            {
                Task.Delay(timeout.Value).ContinueWith(
                task =>
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    var message = $"'\"{fileName}\" {commandLineArgs}' timeout in directory '{cwd}' after {timeout.Value.Seconds} seconds";
                    tcs.TrySetException(new TimeoutException(message));
                }, TaskScheduler.Default);
            }

            lock (processExited)
            {
                process.Start();

                // Thread.Sleep(10000);
                // BeginOutputReadLine() and Exited event handler may have competition issue, above code can easily reproduce this problem
                // Add lock to ensure the locked area code can be always exected before exited event
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return tcs.Task;
        }

        /// <summary>
        /// Provide a process lock function based on locking file
        /// </summary>
        /// <param name="lockRelativePath">The lock file relative path</param>
        /// <param name="action">The action you want to lock</param>
        /// <param name="retry">The retry count, default is 600 times</param>
        /// <param name="retryTimeSpanInterval">The retry interval, default is 1 seconds</param>
        /// <returns>The task status</returns>
        public static async Task ProcessLock(string lockRelativePath, Func<Task> action, int retry = 600, TimeSpan? retryTimeSpanInterval = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(lockRelativePath));
            Debug.Assert(!Path.IsPathRooted(lockRelativePath));

            var lockPath = Path.Combine(s_lockDir, lockRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath));
            using (var lockFile = await AcquireFileStreamLock(lockPath, retry < 0 ? 0 : retry, retryTimeSpanInterval ?? TimeSpan.FromSeconds(1)))
            {
                await action();
            }
        }

        private static async Task<FileStream> AcquireFileStreamLock(string lockPath, int retry, TimeSpan retryTimeSpanInterval)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    return new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                }
                catch when (retryCount++ < retry)
                {
                    // TODO: error handling
                    // TODO: notify user current waiting process
                    await Task.Delay(retryTimeSpanInterval);
                }
            }
        }
    }
}
