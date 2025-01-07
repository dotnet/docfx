// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Docfx.Tests.Common;

public class TestBase : IClassFixture<TestBase>, IDisposable
{
    private readonly List<string> _folderCollection = [];
    private readonly object _locker = new();

    protected string GetRandomFolder()
    {
        var folder = GetFolder();

        lock (_locker)
        {
            _folderCollection.Add(folder);
        }

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string GetFolder()
    {
        var folder = Path.GetRandomFileName();
        if (Directory.Exists(folder))
        {
            folder += DateTime.Now.ToString("HHmmssffff");
            if (Directory.Exists(folder))
            {
                throw new InvalidOperationException($"Random folder name collides {folder}");
            }
        }
        return folder;
    }

    protected static string CreateFile(string fileName, string[] lines, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var dir = Path.GetDirectoryName(fileName);
        CreateDirectory(dir, baseFolder);
        var file = Path.Combine(baseFolder, fileName);
        File.WriteAllLines(file, lines);
        return file;
    }

    protected static string CreateFile(string fileName, string content, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(baseFolder);

        var dir = Path.GetDirectoryName(fileName);
        CreateDirectory(dir, baseFolder);
        var file = Path.Combine(baseFolder, fileName);
        File.WriteAllText(file, content);
        return file.Replace('\\', '/');
    }

    protected static string UpdateFile(string fileName, string[] lines, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(baseFolder);

        File.Delete(Path.Combine(baseFolder, fileName));
        return CreateFile(fileName, lines, baseFolder);
    }

    protected static string CreateDirectory(string dir, string baseFolder)
    {
        if (string.IsNullOrEmpty(dir))
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(baseFolder);

        var subDirectory = Path.Combine(baseFolder, dir);
        Directory.CreateDirectory(subDirectory);
        return subDirectory;
    }

    public static void AssertJsonEquivalent(string expected, string actual)
    {
        AssertJsonEquivalent(
            JsonDocument.Parse(expected, new() { AllowTrailingCommas = true }).RootElement,
            JsonDocument.Parse(actual, new() { AllowTrailingCommas = true }).RootElement);
    }

    public static void AssertJsonEquivalent(JsonElement expected, JsonElement actual)
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in expected.EnumerateObject())
                    AssertJsonEquivalent(property.Value, actual.GetProperty(property.Name));
                break;

            case JsonValueKind.Array:
                for (var i = 0; i < expected.GetArrayLength(); i++)
                    AssertJsonEquivalent(expected[i], actual[i]);
                break;

            default:
                Assert.Equal(expected.GetRawText(), actual.GetRawText());
                break;
        }
    }


    public virtual void Dispose()
    {
        try
        {
            foreach (var folder in _folderCollection)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
        catch
        {
        }
    }
}
