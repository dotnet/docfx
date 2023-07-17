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

    public static string[] ReadAllLines(this IFileAbstractLayer fal, string file)
    {
        using var reader = OpenReadText(fal, file);
        string line;
        var list = new List<string>();
        while ((line = reader.ReadLine()) != null)
        {
            list.Add(line);
        }
        return list.ToArray();
    }

    public static StreamWriter CreateText(this IFileAbstractLayer fal, string file) =>
        new(fal.Create(file));

    public static void WriteAllText(this IFileAbstractLayer fal, string file, string content)
    {
        using var writer = CreateText(fal, file);
        writer.Write(content);
    }

    public static bool HasProperty(this IFileAbstractLayer fal, string file, string propertyName)
    {
        var dict = fal.GetProperties(file);
        return dict.ContainsKey(propertyName);
    }

    public static string GetProperty(this IFileAbstractLayer fal, string file, string propertyName)
    {
        var dict = fal.GetProperties(file);
        dict.TryGetValue(propertyName, out string result);
        return result;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetAllPhysicalPaths(this IFileAbstractLayer fal) =>
        from r in fal.GetAllInputFiles()
        select new KeyValuePair<string, string>(r, fal.GetPhysicalPath(r));
}
