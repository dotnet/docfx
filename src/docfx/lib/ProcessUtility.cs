// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Docs
{
    internal static class ProcessUtility
    {
        /// <summary>
        /// Execute process with args
        /// </summary>
        /// <param name="fileName">The process path name or location</param>
        /// <param name="commandLineArgs">The process command line args</param>
        /// <param name="cwd">The current working directory</param>
        /// <param name="timeout">The timeout setting</param>
        /// <returns>The executed result</returns>
        public static Task<string> Execute(string fileName, string commandLineArgs, string cwd = null, TimeSpan? timeout = null)
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

            process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
            process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

            process.Exited += (a, b) =>
            {
                // Wait for exit here to ensure the standard output/error is flushed.
                process.WaitForExit();

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

            Task.Delay(timeout ?? TimeSpan.FromMinutes(5)).ContinueWith(
                task =>
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    tcs.TrySetCanceled();
                }, TaskScheduler.Default);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
