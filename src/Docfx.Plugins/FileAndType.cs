// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public sealed class FileAndType
    : IEquatable<FileAndType>
{
    [Newtonsoft.Json.JsonConstructor]
    [System.Text.Json.Serialization.JsonConstructor]
    public FileAndType(string baseDir, string file, DocumentType type, string sourceDir = null, string destinationDir = null)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(file);

        if (!Path.IsPathRooted(baseDir))
        {
            throw new ArgumentException("Base directory must be rooted.", nameof(baseDir));
        }
        if (string.IsNullOrWhiteSpace(file))
        {
            throw new ArgumentException("File cannot be empty or whitespace.", nameof(file));
        }
        if (Path.IsPathRooted(file))
        {
            throw new ArgumentException("File cannot be rooted.", nameof(file));
        }
        if (!string.IsNullOrEmpty(sourceDir) && Path.IsPathRooted(sourceDir))
        {
            throw new ArgumentException("File cannot be rooted.", nameof(sourceDir));
        }
        if (!string.IsNullOrEmpty(destinationDir) && Path.IsPathRooted(destinationDir))
        {
            throw new ArgumentException("File cannot be rooted.", nameof(destinationDir));
        }

        BaseDir = baseDir.Replace('\\', '/');
        File = file.Replace('\\', '/');
        Type = type;
        FullPath = Path.Combine(BaseDir, File).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        SourceDir = sourceDir?.Replace('\\', '/') ?? string.Empty;
        DestinationDir = destinationDir?.Replace('\\', '/') ?? string.Empty;
        StringComparer = GetStringComparer();
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public StringComparer StringComparer { get; }

    [JsonProperty("baseDir")]
    [JsonPropertyName("baseDir")]
    public string BaseDir { get; }

    [JsonProperty("file")]
    [JsonPropertyName("file")]
    public string File { get; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string FullPath { get; }

    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public DocumentType Type { get; }

    [JsonProperty("sourceDir")]
    [JsonPropertyName("sourceDir")]
    public string SourceDir { get; set; }

    [JsonProperty("destinationDir")]
    [JsonPropertyName("destinationDir")]
    public string DestinationDir { get; set; }

    public FileAndType ChangeBaseDir(string baseDir)
    {
        return new FileAndType(baseDir, File, Type, SourceDir, DestinationDir);
    }

    public FileAndType ChangeFile(string file)
    {
        return new FileAndType(BaseDir, file, Type, SourceDir, DestinationDir);
    }

    public FileAndType ChangeType(DocumentType type)
    {
        return new FileAndType(BaseDir, File, type, SourceDir, DestinationDir);
    }

    public bool Equals(FileAndType other)
    {
        if (other == null)
        {
            return false;
        }
        return StringComparer.Equals(File, other.File) &&
            Type == other.Type &&
            StringComparer.Equals(BaseDir, other.BaseDir);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as FileAndType);
    }

    public override int GetHashCode()
    {
        return StringComparer.GetHashCode(File) + (int)Type ^ StringComparer.GetHashCode(BaseDir);
    }

    public static bool operator ==(FileAndType left, FileAndType right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null)
        {
            return false;
        }
        return left.Equals(right);
    }

    public static bool operator !=(FileAndType left, FileAndType right)
    {
        return !(left == right);
    }

    private static StringComparer GetStringComparer()
    {
        if (Environment.OSVersion.Platform < PlatformID.Unix)
        {
            return StringComparer.OrdinalIgnoreCase;
        }
        else
        {
            return StringComparer.Ordinal;
        }
    }
}
