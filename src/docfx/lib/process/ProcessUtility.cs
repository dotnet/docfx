// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using O9d.Json.Formatting;

namespace Microsoft.Docs.Build;

internal static class ProcessUtility
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
        WriteIndented = true,
    };

    /// <summary>
    /// Start a new process and wait for its execution to complete
    /// </summary>
    public static string Execute(
        string fileName, string commandLineArgs, string? cwd = null, bool stdout = true, string? secret = null, IReadOnlyDictionary<string, string>? env = null)
    {
        var sanitizedCommandLineArgs = MaskUtility.HideSecret(commandLineArgs, secret);

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

            if (env != null)
            {
                foreach (var (key, value) in env)
                {
                    psi.EnvironmentVariables[key] = value;
                }
            }

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
                var sanitizedErrorData = MaskUtility.HideSecret(errorData, secret);

                throw new InvalidOperationException(
                    $"'\"{fileName}\" {sanitizedCommandLineArgs}' failed in directory '{cwd}' with exit code {process.ExitCode}: " +
                    $"\nSTDOUT:'{result}': \nSTDERR:'{sanitizedErrorData}'");
            }

            return result.ToString();
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
    /// When used together with <see cref="WriteJsonFile(string,string)"/>, provides inter-process synchronized access to the file.
    /// </summary>
    public static T ReadJsonFile<T>(string path) where T : class, new()
    {
        byte[] bytes;
        using (InterProcessMutex.Create(path))
        {
            bytes = File.ReadAllBytes(path);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, s_jsonOptions) ?? new();
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
    /// When used together with <see cref="ReadJsonFile{T}(string)"/>, provides inter-process synchronized access to the file.
    /// </summary>
    public static void WriteJsonFile<T>(string path, T data)
    {
        using (InterProcessMutex.Create(path))
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024, FileOptions.SequentialScan))
        {
            JsonSerializer.Serialize(fs, data, s_jsonOptions);
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
