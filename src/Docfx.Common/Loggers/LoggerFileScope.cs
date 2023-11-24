// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public sealed class LoggerFileScope : IDisposable
{
    private static readonly AsyncLocal<string> t_fileName = new();
    private readonly string _originFileName;

    public LoggerFileScope(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or white space.", nameof(fileName));
        }
        _originFileName = GetFileName();
        SetFileName(fileName);
    }

    public void Dispose()
    {
        SetFileName(_originFileName);
    }

    internal static string GetFileName() => t_fileName.Value;

    private static void SetFileName(string fileName) => t_fileName.Value = fileName;
}
