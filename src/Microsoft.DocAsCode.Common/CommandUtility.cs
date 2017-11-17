// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CommandUtility
    {
        public static int RunCommand(CommandInfo commandInfo, StreamWriter stdoutWriter = null, StreamWriter stderrWriter = null, int timeoutInMilliseconds = Timeout.Infinite)
        {
            if (commandInfo == null)
            {
                throw new ArgumentNullException(nameof(commandInfo));
            }

            if (timeoutInMilliseconds < 0 && timeoutInMilliseconds != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInMilliseconds), $"{nameof(timeoutInMilliseconds)} must be equal to or greater than 0, or equal to {Timeout.Infinite}.");
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = commandInfo.Name;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = commandInfo.Arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = commandInfo.WorkingDirectory;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                Task outputTask = null;
                if (stdoutWriter != null)
                {
                    outputTask = Task.Run(() =>
                    {
                        var buffer = new char[512];
                        while (true)
                        {
                            var readCount = process.StandardOutput.Read(buffer, 0, 512);
                            if (readCount > 0)
                            {
                                try
                                {
                                    stdoutWriter.Write(buffer, 0, readCount);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"Unable to write standard output when running command {commandInfo.Name}: {ex.Message}");
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    });
                }

                Task errorTask = null;
                if (stderrWriter != null)
                {
                    errorTask = Task.Run(() =>
                    {
                        var buffer = new char[512];
                        while (true)
                        {
                            var index = process.StandardError.Read(buffer, 0, 512);
                            if (index > 0)
                            {
                                try
                                {
                                    stderrWriter.Write(buffer, 0, index);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"Unable to write standard error output when running command {commandInfo.Name}: {ex.Message}");
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    });
                }

                try
                {
                    if (process.WaitForExit(timeoutInMilliseconds))
                    {
                        return process.ExitCode;
                    }
                    else
                    {
                        Logger.LogWarning($"Timeout ({timeoutInMilliseconds}ms) exceeded. Killing process {process.ProcessName}.");
                        process.Kill();
                        process.WaitForExit();
                    }
                }
                finally
                {
                    outputTask?.Wait();
                    errorTask?.Wait();
                }
            }
            return 0;
        }

        public static bool ExistCommand(string commandName, Action<string> processOutput = null, Action<string> processError = null)
        {
            int exitCode;
            using (var outputStream = new MemoryStream())
            using (var errorStream = new MemoryStream())
            {
                using (var outputStreamWriter = new StreamWriter(outputStream))
                using (var errorStreamWriter = new StreamWriter(errorStream))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        exitCode = RunCommand(new CommandInfo
                        {
                            // type is a bash command, hence should be an argument to 'bash'
                            Name = "bash",
                            Arguments = $"-c \"type {commandName}\""
                        }, outputStreamWriter, errorStreamWriter, timeoutInMilliseconds: 60000);
                    }
                    else
                    {
                        exitCode = RunCommand(new CommandInfo
                        {
                            Name = "where",
                            Arguments = commandName
                        }, outputStreamWriter, errorStreamWriter, timeoutInMilliseconds: 60000);
                    }

                    // writer streams have to be flushed before reading from memory streams
                    // make sure that streamwriter is not closed before reading from memory stream
                    outputStreamWriter.Flush();
                    errorStreamWriter.Flush();

                    var outputString = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
                    var errorString = System.Text.Encoding.UTF8.GetString(errorStream.ToArray());

                    // Allow caller to decide what to do with the output and error logs 
                    processOutput?.Invoke(outputString);
                    processError?.Invoke(errorString);
                }
            }
            return exitCode == 0;
        }
    }

    public class CommandInfo
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
    }
}
