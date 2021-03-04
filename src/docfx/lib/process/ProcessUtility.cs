// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
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
        /// Start a new process and wait for its execution to complete
        /// </summary>
        public static string Execute(string fileName, string commandLineArgs, string? cwd = null, bool stdout = true, string? secret = null)
        {
            var sanitizedCommandLineArgs = secret != null ? HideSecret(commandLineArgs, secret) : commandLineArgs;

            using (PerfScope.Start($"Executing '\"{fileName}\" {sanitizedCommandLineArgs}' in '{Path.GetFullPath(cwd ?? ".")}'"))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = cwd ?? ".",
                    Arguments = commandLineArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = stdout,
                    RedirectStandardError = true,
                };

                using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName}");

                var error = new StringBuilder();
                var result = new StringBuilder();

                var pipeError = Task.Run(() => PipeStream(process.StandardError, Console.Out, new StringWriter(error)));
                var pipeOutput = stdout
                    ? Task.Run(() => PipeStream(process.StandardOutput, new StringWriter(result)))
                    : Task.CompletedTask;

                Task.WhenAll(process.WaitForExitAsync(), pipeError, pipeOutput).GetAwaiter().GetResult();

                if (process.ExitCode != 0)
                {
                    var errorData = error.ToString();
                    var sanitizedErrorData = secret != null ? HideSecret(errorData, secret) : errorData;

                    throw new InvalidOperationException(
                        $"'\"{fileName}\" {sanitizedCommandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: " +
                        $"\nSTDOUT:'{result}': \nSTDERR:'{sanitizedErrorData}'");
                }

                return result.ToString();
            }

            static string HideSecret(string arg, string secret)
            {
                return arg.Replace(secret, secret.Length > 10 ? secret[0..2] + "***" + secret[^2..] : "***");
            }

            static void PipeStream(TextReader input, TextWriter output1, TextWriter? output2 = null)
            {
                var buffer = ArrayPool<char>.Shared.Rent(1024);

                while (input.Read(buffer, 0, buffer.Length) is var length && length > 0)
                {
                    output1.Write(buffer, 0, length);
                    output2?.Write(buffer, 0, length);
                }

                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Reads the content of a file.
        /// When used together with <see cref="WriteFile(string,string)"/>, provides inter-process synchronized access to the file.
        /// </summary>
        public static T ReadJsonFile<T>(string path) where T : class, new()
        {
            var content = "";
            using (InterProcessMutex.Create(path))
            {
                content = File.ReadAllText(path);
            }

            try
            {
                return JsonUtility.DeserializeData<T>(content, new FilePath(path));
            }
            catch (Exception ex)
            {
                Log.Important($"Ignore data file due to a problem reading '{path}'.", ConsoleColor.Yellow);
                Log.Write(ex);
                return new T();
            }
        }

        public static void ReadFile(string path, Action<Stream> read)
        {
            using (InterProcessMutex.Create(path))
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
            {
                try
                {
                    read(fs);
                }
                catch (Exception ex)
                {
                    Log.Important($"Ignore data file due to a problem reading '{path}'.", ConsoleColor.Yellow);
                    Log.Write(ex);
                }
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
