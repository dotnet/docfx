// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public static class FileAbstractLayerExtensions
{
    public static StreamReader OpenReadText(this IFileAbstractLayer fal, string file) =>
        new(fal.OpenRead(file));

    public static string ReadAllText(this IFileAbstractLayer fal, string file)
    {
        using var reader = OpenReadText(fal, file);
        return reader.ReadToEnd();
    }
}
