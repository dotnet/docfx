// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.MarkdigEngine.Extensions;
using Markdig;
using Markdig.Syntax;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public static class TestUtility
{
    public static string VerifyMarkup(
        string markdown,
        string html,
        string[] errors = null,
        string[] dependencies = null,
        bool lineNumber = false,
        string filePath = "test.md",
        Dictionary<string, string> tokens = null,
        Dictionary<string, string> files = null,
        MarkdigExtensionSetting[] optionalExtensions = null,
        Dictionary<string, string> notes = null,
        PlantUmlOptions plantUml = null)
    {
        errors ??= [];
        tokens ??= [];
        files ??= [];
        optionalExtensions ??= [];

        var actualErrors = new List<string>();
        var actualDependencies = new HashSet<string>();

        var markdownContext = new MarkdownContext(
            getToken: key => tokens.GetValueOrDefault(key),
            logInfo: (a, b, c, d) => { },
            logSuggestion: Log("suggestion"),
            logWarning: Log("warning"),
            logError: Log("error"),
            readFile: ReadFile);

        var pipelineBuilder = new MarkdownPipelineBuilder()
            .UseDocfxExtensions(markdownContext, notes, plantUml)
            .UseYamlFrontMatter()
            .UseOptionalExtensions(optionalExtensions);

        if (lineNumber)
        {
            pipelineBuilder.UseLineNumber();
        }

        var pipeline = pipelineBuilder.Build();

        using (InclusionContext.PushFile(filePath))
        {
            var actualHtml = Markdown.ToHtml(markdown, pipeline);

            if (html != null)
            {
                Assert.Equal(
                    html.Replace("\r", "").Replace("\n", ""),
                    actualHtml.Replace("\r", "").Replace("\n", ""));
            }

            Assert.Equal(errors.OrderBy(_ => _), actualErrors.OrderBy(_ => _));

            if (dependencies != null)
            {
                Assert.Equal(dependencies.OrderBy(_ => _), actualDependencies.OrderBy(_ => _));
            }
            return actualHtml;
        }

        MarkdownContext.LogActionDelegate Log(string level)
        {
            return (code, message, origin, line) => actualErrors.Add(code);
        }

        (string content, object file) ReadFile(string path, MarkdownObject origin)
        {
            var key = Path.Combine(Path.GetDirectoryName(InclusionContext.File.ToString()), path).Replace('\\', '/');

            if (path.StartsWith("~/"))
            {
                path = path.Substring(2);
                key = path;
            }

            actualDependencies.Add(path);
            return files.TryGetValue(key, out var value) ? (value, key) : default;
        }
    }
}
