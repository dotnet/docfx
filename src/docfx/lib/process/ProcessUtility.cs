// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MemoryStream properly disposed")]
        public static string Execute(string fileName, string commandLineArgs, string? cwd = null, bool stdout = true, string[]? secrets = null)
        {
            var sanitizedCommandLineArgs = secrets != null ? secrets.Aggregate(commandLineArgs, HideSecrets) : commandLineArgs;

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

                using var errorStream = new MemoryStream();
                var readError = Task.Run(() => PipeStream(process.StandardError.BaseStream, Console.OpenStandardOutput(), errorStream));
                var result = stdout ? process.StandardOutput.ReadToEnd() : "";

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    readError.GetAwaiter().GetResult();
                    errorStream.Seek(0, SeekOrigin.Begin);

                    using var errorReader = new StreamReader(errorStream);
                    var errorData = errorReader.ReadToEnd();
                    var sanitizedErrorData = secrets != null ? secrets.Aggregate(errorData, HideSecrets) : errorData;

                    throw new InvalidOperationException(
                        $"'\"{fileName}\" {sanitizedCommandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: " +
                        $"\nSTDOUT:'{result}': \nSTDERR:'{sanitizedErrorData}'");
                }

                return result;
            }

            static string HideSecrets(string arg, string secret)
            {
                return arg.Replace(secret, secret.Length > 10 ? secret[0..3] + "***" + secret[^3..] : "***");
            }

            static void PipeStream(Stream input, Stream output1, Stream output2)
            {
                var console = Console.OpenStandardOutput();
                var buffer = new byte[1024];

                while (true)
                {
                    var bytesRead = input.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                    {
                        break;
                    }

                    output1.Write(buffer, 0, bytesRead);
                    output2.Write(buffer, 0, bytesRead);
                }
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
