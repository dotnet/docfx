// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

#nullable enable

namespace Docfx.Common;

public static class YamlMime
{
    public const string YamlMimePrefix = nameof(YamlMime) + ":";
    public const string ManagedReference = YamlMimePrefix + nameof(ManagedReference);
    public const string TableOfContent = YamlMimePrefix + nameof(TableOfContent);
    public const string XRefMap = YamlMimePrefix + nameof(XRefMap);

    public static string? ReadMime(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var line = reader.ReadLine();
        if (line == null || !line.StartsWith('#'))
        {
            return null;
        }
        var content = line.TrimStart('#').Trim(' ');
        if (!content.StartsWith(YamlMimePrefix, StringComparison.Ordinal))
        {
            return null;
        }
        return content;
    }

    public static string? ReadMime(string file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var path = EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file);
        using var stream = new FileStream(path, CustomFileReadOptions);
        using var reader = new StreamReader(stream, bufferSize: 128); // Min:128, Default:1024
        return ReadMime(reader);
    }

    private static readonly FileStreamOptions CustomFileReadOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 0, // Optimize for small file size (Default:4096)
        Options = OperatingSystem.IsWindows()
                    ? FileOptions.SequentialScan
                    : FileOptions.None, // `File.ReadAllBytes(string path)` use this settings.
    };
}
