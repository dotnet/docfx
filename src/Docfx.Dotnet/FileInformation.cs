// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Dotnet;

internal class FileInformation
{
    public FileType Type { get; }
    public string NormalizedPath { get; }
    public string RawPath { get; }

    public FileInformation(string raw)
    {
        RawPath = raw;
        NormalizedPath = Normalize(raw);
        Type = GetFileType(raw);
    }

    public override int GetHashCode()
    {
        return NormalizedPath?.GetHashCode() ?? 0;
    }

    public override bool Equals(object obj)
    {
        return Equals(NormalizedPath, (obj as FileInformation)?.NormalizedPath);
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return Path.Combine(EnvironmentContext.BaseDirectory, path).ToNormalizedFullPath();
    }

    private static FileType GetFileType(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        switch (extension.ToLowerInvariant())
        {
            case ".sln":
            case ".slnf":
                return FileType.Solution;
            case ".csproj":
            case ".vbproj":
                return FileType.Project;
            case ".cs":
                return FileType.CSSourceCode;
            case ".vb":
                return FileType.VBSourceCode;
            case ".dll":
            case ".exe":
                return FileType.Assembly;
            default:
                return FileType.NotSupported;
        }
    }
}
