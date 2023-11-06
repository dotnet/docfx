// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public sealed class LoggerFileScope : IDisposable
{
    private readonly string _originFileName;

    public LoggerFileScope(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Phase name cannot be null or white space.", nameof(fileName));
        }
        _originFileName = GetFileName();
        SetFileName(fileName);
    }

    public void Dispose()
    {
        SetFileName(_originFileName);
    }

    internal static string GetFileName()
    {
        return LogicalCallContext.GetData(nameof(LoggerFileScope)) as string;
    }

    private static void SetFileName(string fileName)
    {
        LogicalCallContext.SetData(nameof(LoggerFileScope), fileName);
    }
}
