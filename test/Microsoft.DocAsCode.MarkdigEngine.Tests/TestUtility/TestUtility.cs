// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;
using Xunit;

namespace Microsoft.DocAsCode.MarkdigEngine.Tests;

public static class TestUtility
{
    public static void AssertEqual(string expected, string actual, Func<string, MarkupResult> markup)
    {
        var result = markup(actual);
        Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);
    }

    public static void AssertEqual(string expected, string actual, Func<string, string, MarkupResult> markup)
    {
        var result = markup(actual, null);
        Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);
    }

    public static MarkupResult Markup(string content, string filePath = null)
    {
        var parameter = new MarkdownServiceParameters
        {
            BasePath = "."
        };
        var service = new MarkdigMarkdownService(parameter);

        return service.Markup(content, filePath ?? string.Empty);
    }

    public static MarkupResult MarkupWithoutSourceInfo(string content, string filePath = null)
    {
        var parameter = new MarkdownServiceParameters
        {
            BasePath = ".",
            Extensions = new() { EnableSourceInfo = false },
        };
        var service = new MarkdigMarkdownService(parameter);

        return service.Markup(content, filePath ?? string.Empty);
    }

    public static void WriteToFile(string file, string content)
    {
        var dir = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(file, content);
    }

    public static MarkdigMarkdownService CreateMarkdownService()
    {
        var parameter = new MarkdownServiceParameters
        {
            BasePath = ".",
            Extensions = new() { EnableSourceInfo = false },
        };

        return new MarkdigMarkdownService(parameter);
    }
}
