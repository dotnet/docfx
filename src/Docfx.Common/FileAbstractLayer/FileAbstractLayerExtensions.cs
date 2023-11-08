// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public static class FileAbstractLayerExtensions
{
    public static StreamReader OpenReadText(this FileAbstractLayer fal, RelativePath file) =>
        new(fal.OpenRead(file));

    public static string ReadAllText(this FileAbstractLayer fal, RelativePath file)
    {
        using var sr = OpenReadText(fal, file);
        return sr.ReadToEnd();
    }

    public static string ReadAllText(this FileAbstractLayer fal, string file) =>
        ReadAllText(fal, (RelativePath)file);

    public static StreamWriter CreateText(this FileAbstractLayer fal, RelativePath file) =>
        new(fal.Create(file));

    public static void WriteAllText(this FileAbstractLayer fal, RelativePath file, string content)
    {
        using var writer = CreateText(fal, file);
        writer.Write(content);
    }

    public static void WriteAllText(this FileAbstractLayer fal, string file, string content) =>
        WriteAllText(fal, (RelativePath)file, content);

    public static string GetOutputPhysicalPath(this FileAbstractLayer fal, string file) =>
        FileAbstractLayerBuilder.Default
            .ReadFromOutput(fal)
            .Create()
            .GetPhysicalPath(file);
}
