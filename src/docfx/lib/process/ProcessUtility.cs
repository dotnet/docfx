// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide process utility
    /// </summary>
    internal static class ProcessUtility
    {
        /// <summary>
        /// Start a new process and wait for its execution to complete
        /// </summary>
        public static string Execute(string fileName, string commandLineArgs, string? cwd = null, bool stdout = true, string[]? secrets = null)
        {
            var sanitizedCommandLineArgs = secrets != null ? secrets.Aggregate(commandLineArgs, HideSecrets) : commandLineArgs;

            using (PerfScope.Start($"Executing '\"{fileName}\" {sanitizedCommandLineArgs}' in '{Path.GetFullPath(cwd ?? ".")}'"))
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

                // Redirect stderr to stdout
                Task.Run(() => process.StandardError.BaseStream.CopyTo(Console.OpenStandardOutput()));

                var result = stdout ? process.StandardOutput.ReadToEnd() : "";

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"'\"{fileName}\" {sanitizedCommandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: \nSTDOUT:'{result}'");
                }

                return result;
            }

            static string HideSecrets(string arg, string secret)
            {
                return arg.Replace(secret, secret.Length > 5 ? "***" + secret.Substring(secret.Length - 5) : "***");
            }
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="WriteFile(string,string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static string ReadFile(string path)
        {
            using (InterProcessMutex.Create(path))
            {
                return File.ReadAllText(path);
            }
        }

        public static void ReadFile(string path, Action<Stream> read)
        {
            using (InterProcessMutex.Create(path))
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
            {
                read(fs);
            }
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="ReadFile(string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static void WriteFile(string path, string content)
        {
            using (InterProcessMutex.Create(path))
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
            using (var writer = new StreamWriter(fs))
            {
                writer.Write(content);
            }
        }

        public static void WriteFile(string path, Action<Stream> write)
        {
            using (InterProcessMutex.Create(path))
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
            {
                write(fs);
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
    }
}
